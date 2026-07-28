using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FlashSale.Api.OrderBook;
namespace FlashSale.Api.Endpoints;
public static class SaleEndpoints
{    

    // 
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var route = app.MapGroup("/sales");
        
        // will display inve
        route.MapGet("/items", () =>
        {   
            List<string> productList = new List<string>();
            foreach (var product in Inventory.Products)
            {
                var s = $"Product ID: {product.Id}, Name: {product.Name}, Quantity: {product.Quantity}, Price: {product.Price}";
                productList.Add(s);
            }
            return Results.Ok(productList);
        });
         
        // Manually update inventory from CSV file, this endpoint can be used to refresh the inventory without restarting the application.
        // !! Danger, need to test will it affect ongoing orders if inventory is updated while orders are being processed.
         route.MapGet("/resetInventory", () =>
        {   
            var inventory = app.ServiceProvider.GetRequiredService<Inventory>();
            inventory.PopulateInventory();
            List<string> productList = new List<string>();
            foreach (var product in Inventory.Products)
            {
                var s = $"Product ID: {product.Id}, Name: {product.Name}, Quantity: {product.Quantity}, Price: {product.Price}";
                productList.Add(s);
            }
            return Results.Ok(productList);
        });

      

        route.MapPost("/order", (Sale sale) =>
        {
            // Save sale
            return Results.Created($"/sales/{sale.Id}", sale);
        });

        return app;
    }
}

public record Sale(int Id, int ProductId, int Quantity, decimal TotalPrice);
