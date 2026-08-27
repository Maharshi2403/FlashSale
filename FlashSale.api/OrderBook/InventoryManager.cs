using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Disruptor;
using FlashSale.Api.Hubs;
using FlashSale.Api.OrderBook.OrderEvent;
using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;
using Microsoft.AspNetCore.SignalR;
namespace FlashSale.Api.OrderBook.InventoryManager;


public class InventoryManager
{  
    
   private readonly System.Collections.Concurrent.ConcurrentDictionary<int, Product> _inventory;
   private readonly string inventoryFilePath;

   public InventoryManager(string contentRootPath)
    {
        _inventory = new ();
        inventoryFilePath = Path.Combine(contentRootPath, "OrderBook", "itemlist.csv");

    }

   // populate inventory with products
   public void PopulateInventory(){
        var candidates = new string[] {
             inventoryFilePath,
             Path.Combine(AppContext.BaseDirectory ?? string.Empty, "OrderBook", "itemlist.csv"),
             Path.Combine(Directory.GetCurrentDirectory(), "OrderBook", "itemlist.csv"),
             "OrderBook/itemlist.csv",
        };

        var foundPath = candidates.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p));
        if (foundPath == null)
        {
            Console.WriteLine($"Inventory file not found. Checked paths:");
            foreach(var p in candidates)
            {
                 Console.WriteLine($" - {p}");
            }
            return;
        }

        Console.WriteLine($"Using inventory file: {foundPath}");
        _inventory.Clear();

        var lines = File.ReadAllLines(foundPath);
        int i = 0;
      foreach(var line in lines){
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SplitCsvLine(line);

            // skip CSV header if present
            if (parts.Length > 0 && parts[0].Trim().Equals("id", StringComparison.OrdinalIgnoreCase)) continue;

            Product? product = null;

            if (parts.Length >= 7)
            {
                // new format: id,name,category,description,price,stock,specs
                try
                {
                    var specs = new Dictionary<string, string>();
                    var specsRaw = parts[6].Trim();
                    if (!string.IsNullOrEmpty(specsRaw) && specsRaw != "{}")
                    {
                        if (specsRaw.StartsWith("{"))
                        {
                            try { specs = JsonSerializer.Deserialize<Dictionary<string,string>>(specsRaw) ?? new Dictionary<string,string>(); } catch { specs = new Dictionary<string,string>(); }
                        }
                        else
                        {
                            // fallback simple format: key=value;key2=value2
                            try
                            {
                                foreach (var kv in specsRaw.Split(new[] {';','|'}, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    var kvp = kv.Split('=', 2);
                                    if (kvp.Length == 2) specs[kvp[0].Trim()] = kvp[1].Trim();
                                }
                            }
                            catch { specs = new Dictionary<string,string>(); }
                        }
                    }

                    product = new Product
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1],
                        Category = parts[2],
                        Description = parts[3],
                        Price = decimal.Parse(parts[4]),
                        Quantity = int.Parse(parts[5]),
                        Specs = specs
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid line in inventory file (new format): {line} -> {ex.Message}");
                    continue;
                }
            }
            else if (parts.Length == 4)
            {
                try
                {
                    product = new Product
                    {
                        Id = int.Parse(parts[0]),
                        Name = parts[1],
                        Quantity = int.Parse(parts[2]),
                        Price = decimal.Parse(parts[3])
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid line in inventory file (legacy format): {line} -> {ex.Message}");
                    continue;
                }
            }
            else
            {
                Console.WriteLine($"Invalid line in inventory file: {line}");
                continue;
            }

            _inventory[product.Id] = product;
            i++;
        }

    }
     // make reservation
     public string? TryReserve(int productId, int quantity)
    {
        if (quantity <= 0)
            return null;

        while (true)
        {
            if (!_inventory.TryGetValue(productId, out var current))
                return null; // Product doesn't exist
                
            if (current.Quantity < quantity)
                return null; // Out of stock

            var updated = new Product
            {
                Id = current.Id,
                Name = current.Name,
                Category = current.Category,
                Description = current.Description,
                Price = current.Price,
                Quantity = current.Quantity - quantity,
                Specs = current.Specs
            };

            if (_inventory.TryUpdate(productId, updated, current))
                return $"{productId}-{DateTime.UtcNow.Ticks}"; // Reservation token
        }
    }
    /// <summary>
    /// Release reservation on failure.
    /// </summary>
    public void Release(int productId, int quantity)
    {
        while (true)
        {
            var current = _inventory[productId];
            var update = new Product
            {
                Id = current.Id,
                Name = current.Name,
                Category = current.Category,
                Description = current.Description,
                Price = current.Price,
                Quantity = current.Quantity + quantity,
                Specs = current.Specs
            };
            if (_inventory.TryUpdate(productId, update, current))
                break;
        }
    }
    // get stock 
    public int GetStock(int productId)
        => _inventory.TryGetValue(productId, out var stock) ? stock.Quantity : 0;

    public IReadOnlyCollection<Product> Products => _inventory.Values.ToArray();

    // Naive CSV splitter that respects quoted commas
    private static string[] SplitCsvLine(string line)
    {
        var parts = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int idx = 0; idx < line.Length; idx++)
        {
            var ch = line[idx];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (ch == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }
        parts.Add(current.ToString());
        return parts.ToArray();
     

   }


}

public class OrderQueue
{
    private Queue<OrderEventMessage> queue = new Queue<OrderEventMessage>();
    public List<OrderEventMessage> Orders { get; } = new List<OrderEventMessage>();

    public void Enqueue(OrderEventMessage sale)
    {
        queue.Enqueue(sale);
        Orders.Add(sale);
    }

    public OrderEventMessage Dequeue()
    {
        return queue.Dequeue();
    }

    public bool IsEmpty()
    {
        return queue.Count == 0;
    }
}



// ============================================================================
// EVENT HANDLERS
// ============================================================================

/// <summary>
/// Handler 1: Validate order (basic checks).
/// </summary>
public class OrderValidationHandler : IEventHandler<OrderEventMessage>
{
 
    public void OnEvent(OrderEventMessage data, long sequence, bool endOfBatch)
    {
        if (data.State != OrderState.PENDING)
            return;
            
        // Validation logic
        if (data.Quantity <= 0 || data.Price <= 0)
        {
            data.State = OrderState.FAILED;
            return;
        }
        
        data.State = OrderState.VALIDATED;
        // Console.WriteLine("Order state: "+ data.State); 
    }
}

/// <summary>
/// Handler 2: Reserve inventory atomically.
/// </summary>
public class InventoryReservationHandler : IEventHandler<OrderEventMessage>
{
    private readonly InventoryManager _inventory;
    private readonly IHubContext<InventoryHub> _hub;
    
    public InventoryReservationHandler(InventoryManager inventory, IHubContext<InventoryHub> hub)
    {
        _inventory = inventory;
        _hub = hub;
    }
    
    public void OnEvent(OrderEventMessage data, long sequence, bool endOfBatch)
    {
        if (data.State != OrderState.VALIDATED)
            return;
            
        var token = _inventory.TryReserve(data.ProductId, data.Quantity);
        // Console.WriteLine("confirmation: " + token);
        if (token != null)
        {
            data.ReservationToken = token;
            data.InventoryReserved = data.Quantity;
            data.State = OrderState.INVENTORY_RESERVED;
            _ = _hub.Clients.All.SendAsync(
                "StockUpdated",
                new StockUpdate(data.ProductId, _inventory.GetStock(data.ProductId)));
        }
        else
        {
            data.State = OrderState.FAILED;
        }
    }
}


/// <summary>
/// Handler 4: Final completion and logging.
/// </summary>
public class CompletionHandler : IEventHandler<OrderEventMessage>
{
    private readonly InventoryManager _inventory;
    private readonly Process _process = Process.GetCurrentProcess();
    private long _successCount = 0;
    private long _failureCount = 0;
    
    public CompletionHandler(InventoryManager inventory)
    {
        _inventory = inventory;
    }
    
    public void OnEvent(OrderEventMessage data, long sequence, bool endOfBatch)
    {
        var elapsedMilliseconds = (Stopwatch.GetTimestamp() - data.Timestamp) * 1000.0 / Stopwatch.Frequency;
        _process.Refresh();
        var cpuMilliseconds = _process.TotalProcessorTime.TotalMilliseconds;
        var workingSetMegabytes = _process.WorkingSet64 / (1024.0 * 1024.0);
        var managedHeapMegabytes = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        if (data.State == OrderState.INVENTORY_RESERVED)
        {
            data.State = OrderState.COMPLETED;
            Interlocked.Increment(ref _successCount);
            
            // Log completed order
            Console.WriteLine($"Order {data.OrderId}: COMPLETED | processing={elapsedMilliseconds:F3} ms | CPU total={cpuMilliseconds:F1} ms | working set={workingSetMegabytes:F1} MB | managed heap={managedHeapMegabytes:F1} MB");
        }
        else if (data.State == OrderState.FAILED)
        {
            Interlocked.Increment(ref _failureCount);
            
            // Release reserved inventory
            if (data.InventoryReserved > 0)
                _inventory.Release(data.ProductId, data.InventoryReserved);
                
            Console.WriteLine($"Order {data.OrderId}: FAILED | processing={elapsedMilliseconds:F3} ms | CPU total={cpuMilliseconds:F1} ms | working set={workingSetMegabytes:F1} MB | managed heap={managedHeapMegabytes:F1} MB");
        }
    }
    
    public long GetSuccessCount() => Interlocked.Read(ref _successCount);
    public long GetFailureCount() => Interlocked.Read(ref _failureCount);
}