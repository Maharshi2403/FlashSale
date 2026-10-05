using System.Collections.Concurrent;
using Model.Product;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System.Linq;
using System.ComponentModel;


namespace Inventory.InventorySchema;

public class InventorySchema
{
    public readonly ConcurrentDictionary<int,Product> _inventory;
    

    public InventorySchema()
    {

        Console.WriteLine("Controle here");
        _inventory = new();
        PopulateInventory(null);
    }

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

    public async Task<bool> TryReserv(int productId, int qty)
    {
        if(_inventory[productId] is not null && _inventory[productId]._Qty > 0)
        {
            _inventory[productId]._Qty--;
            return true;
        }
       
        return false;


    } 


    // release reservation 

    public async Task<bool> TryRelease(int productId, int qty)
    {

        if(_inventory[productId] is not null)
        {
             _inventory[productId]._Qty++;
            return true;
        }

        return false;
    }

    
    

}

