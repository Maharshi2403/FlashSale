using System;
using System.IO;
namespace FlashSale.Api.OrderBook;

public class Inventory
{  
   public static List<Product> Products { get; set; } = new List<Product>();
   public Inventory()
   {
   }

   // populate inventory with products
   public void PopulateInventory(){
      string inventorylist = "OrderBook/itemlist.csv";

      if(!File.Exists(inventorylist)){
         Console.WriteLine($"Inventory file '{inventorylist}' not found.");
         return;
      }
      Products.Clear();
      
      var lines = File.ReadAllLines(inventorylist);
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
    
            Products.Add(product);
        }
     

   }


}

public class Sale
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string userId { get; set; } // Assuming userId is a string, adjust the type as needed

    public Sale(int id, int productId, int quantity, decimal totalPrice, string userId)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        TotalPrice = totalPrice;
        userId = userId;
    }
}

public class OrderQue
{
    private Queue<Sale> queue = new Queue<Sale>();

    public void Enqueue(Sale sale)
    {
        queue.Enqueue(sale);
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