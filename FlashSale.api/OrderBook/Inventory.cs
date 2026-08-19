using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace FlashSale.Api.OrderBook.Inventory;

public class Inventory
{  
    
   public Dictionary<int, Product>  dic;
   private readonly string inventoryFilePath;

   public Inventory(string contentRootPath)
    {
        dic = new Dictionary<int, Product>();
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
        dic.Clear();

        var lines = File.ReadAllLines(foundPath);
      int i = 0;
      foreach(var line in lines){
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = SplitCsvLine(line);

            // skip CSV header if present
            if (parts.Length > 0 && parts[0].Trim().Equals("id", StringComparison.OrdinalIgnoreCase)) continue;

            Product product = null;

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

            dic[i] = product;
            i++;
        }

    }

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

public class Sale
{

    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string userId { get; set; } // Assuming userId is a string, adjust the type as needed
    public long? ProcessingTimeNanoseconds { get; set; }
    public double? ProcessingTimeMicroseconds { get; set; }

    public Sale( int productId, int quantity, decimal totalPrice, string userId)
    {
       
        ProductId = productId;
        Quantity = quantity;
        TotalPrice = totalPrice;
        this.userId = userId;
    }
}

public class OrderQueue
{
    private Queue<Sale> queue = new Queue<Sale>();
    public List<Sale> Orders { get; } = new List<Sale>();

    public void Enqueue(Sale sale)
    {
        queue.Enqueue(sale);
        Orders.Add(sale);
    }

    public Sale Dequeue()
    {
        return queue.Dequeue();
    }

    public bool IsEmpty()
    {
        return queue.Count == 0;
    }
}