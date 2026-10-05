namespace Model.Product;

public class Product
{
    public int _Id { get; set; }
    public string _Name { get; set; }

    public string _Catagory {get; set;}

    public string _Type {get; set;}

    public int _Qty {get; set;}

    public int? _Price { get; set; }
    public string? _Description { get; set; }
    
    // Named tuples make your Specs list much easier to use
    public string? _Specs { get; set; }

    public Product()
    {
        
    }

    // Constructor
    public Product(int productId, string productName, int? price, string? description, string? specs)
    {
        _Id = productId;
        _Name = productName;
        _Price = price;
        _Description = description;
        
        // Safely handle nullability when instantiating the list
        _Specs = specs != null ? specs : null;
    }
}
