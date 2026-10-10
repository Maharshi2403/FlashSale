namespace Model.Product;

public class Product
{
    public int _Id { get; set; }
    public string _Name { get; set; }

    public string _Category {get; set;}
    
    public string _Type {get; set;}

    public int _Qty {get; set;}

    public decimal _Price { get; set; }
    public string? _Description { get; set; }
    
    // Named tuples make your Specs list much easier to use
    public string? _Specs { get; set; }

    public Product()
    {
        
    }

    // Constructor
    public Product(int productId, string productName, string category, string type, int qty, decimal price, string? description, string? specs)
    {
        _Id = productId;
        _Name = productName;
        _Category = category;
        _Type = type;
        _Qty = qty;
        _Price = price;
        
        _Description = description != null ? description : null;
        _Specs = specs != null ? specs : null;
    }
}
