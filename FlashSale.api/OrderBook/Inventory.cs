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