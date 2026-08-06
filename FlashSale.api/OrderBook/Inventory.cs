using System;
using System.IO;
using System.Linq;
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
            var parts = line.Split(',');
            if(parts.Length != 4){
                Console.WriteLine($"Invalid line in inventory file: {line}");
                continue;
            }
            
            var product = new Product
            {
                Id = int.Parse(parts[0]),
                Name = parts[1],
                Quantity = int.Parse(parts[2]),
                Price = decimal.Parse(parts[3])
            };
    
            dic[i] = product;
            i++;
        }
     

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