using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FlashSale.Api.OrderBook.InventoryManager;

namespace FlashSale.Api.Endpoints;

public record PlaceOrderRequest(long UserId, int ProductId, int Quantity, decimal Price);

public static class SaleEndpoints
{    

    // Routes

    // Mentainers
    // ---- resetInventory ==> can alter inventory count

    // Admin
    // ---- items ==> Current available item counts in inventory
    // ---- orderview ==> print out orders that are accepted and added into queue
 
    // user
    // ---- order ==> user can post ordr request from here


    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var route = app.MapGroup("/sales");
        
        // will display inve
        route.MapGet("/items", () =>
        {
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            var productList = inventory.Products.Select(product => new
            {
                id = product.Id,
                name = product.Name,
                category = product.Category,
                description = product.Description,
                price = product.Price,
                stock = product.Quantity,
                specs = product.Specs
            }).ToList();

            return Results.Ok(productList);
        });

        route.MapGet("/orderview", () => Results.Ok(Array.Empty<object>()));


         
        // Manually update inventory from CSV file, this endpoint can be used to refresh the inventory without restarting the application.
        // !! Danger, need to test will it affect ongoing orders if inventory is updated while orders are being processed.

        // good solution - this route should pause POST/order request untile invenotry gets updated 
         route.MapGet("/resetInventory", () =>
        {   
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            inventory.PopulateInventory();
            return Results.Ok(inventory.Products);
        });

      

        route.MapPost("/order", (PlaceOrderRequest order) =>
        {
            var confirm = app.ServiceProvider.GetRequiredService<DisruptorEngine>().PublishOrder(order.UserId, order.ProductId, order.Quantity, order.Price);
            
            return Results.Accepted($"/sales/orders/{confirm}", new { confirm });
        });

        return app;
    }
}


