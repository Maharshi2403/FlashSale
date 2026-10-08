using System.Collections.Concurrent;
using Model.Product;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System.Linq;
using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.ObjectPool;


namespace Inventory.InventorySchema;

public class InventorySchema
{
    public readonly ConcurrentDictionary<int,Product> _inventory;
    

    public InventorySchema()
    {

        _inventory = new();
        PopulateInventory(null);
    }


// Populate inventory from CSV file
    public void PopulateInventory(List<Product>? productList)
    {
        if(productList is null)
        {
            Console.WriteLine("Populating inventory using Data/inventory.csv");

            using var _reader = CsvDataReader.Create("./Data/inventory.csv");

            IEnumerable<Product> records = _reader.GetRecords<Product>();

          
            
            foreach(var record in records)
            {
                _inventory[record._Id] = record;
                 
            }  

        }
    }
    
    // reserv stock in inventory

    public string? TryReserv(int productId, int qty)
    {
        if (qty <= 0)
            return null;

        while (true)
        {
            if (!_inventory.TryGetValue(productId, out var current))
                return null; // Product doesn't exist
                
            if (current._Qty < qty)
                return null; // Out of stock

            var updated = new Product
            {
                _Id = current._Id,
                _Name = current._Name,
                _Category = current._Category,
                _Description = current._Description,
                _Price = current._Price,
                _Qty = current._Qty - qty,
                _Specs = current._Specs
            };

            if (_inventory.TryUpdate(productId, updated, current))
                return $"{productId}-{DateTime.UtcNow.Ticks}"; // Reservation token
        }


    } 


    // release reservation 

    public void TryRelease(int productId, int qty)
    {

        while (true)
        {
            var current = _inventory[productId];
            var update = new Product
            {
                _Id = current._Id,
                _Name = current._Name,
                _Category = current._Category,
               _Description = current._Description,
                _Price = current._Price,
                _Qty = current._Qty + qty,
                _Specs = current._Specs
            };
            if (_inventory.TryUpdate(productId, update, current))
                break;
        }
    }

    
    

}




