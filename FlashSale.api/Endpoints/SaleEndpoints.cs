using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Channels;
using FlashSale.Api.OrderBook;
using FlashSale.Api.OrderBook.Inventory;
using FlashSale.Api.OrderBook.OrderChannel;

namespace FlashSale.Api.Endpoints;
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
            var inventory = app.ServiceProvider.GetRequiredService<Inventory>();
            List<string> productList = new List<string>();
            foreach (var product in inventory.dic.Values)
            {
                var s = $"Product ID: {product.Id}, Name: {product.Name}, Quantity: {product.Quantity}, Price: {product.Price}";
                productList.Add(s);
            }
            return Results.Ok(productList);
        });

        route.MapGet("/orderview", () =>
        {   
            var orderQueue = app.ServiceProvider.GetRequiredService<OrderQueue>();
            List<string> orderList = new List<string>();
            foreach (var order in orderQueue.Orders)
            {
                var s = $"Product ID: {order.ProductId}, Quantity: {order.Quantity}, Total Price: {order.TotalPrice}, User ID: {order.userId}, Processing Time: {order.ProcessingTimeNanoseconds} ns / {order.ProcessingTimeMicroseconds:F3} us";
                orderList.Add(s);
            }
            return Results.Ok(orderList);
        });
         
        // Manually update inventory from CSV file, this endpoint can be used to refresh the inventory without restarting the application.
        // !! Danger, need to test will it affect ongoing orders if inventory is updated while orders are being processed.

        // good solution - this route should pause POST/order request untile invenotry gets updated 
         route.MapGet("/resetInventory", () =>
        {   
            var inventory = app.ServiceProvider.GetRequiredService<Inventory>();
            inventory.PopulateInventory();
            List<string> productList = new List<string>();
            foreach (var product in inventory.dic.Values)
            {
                var s = $"Product ID: {product.Id}, Name: {product.Name}, Quantity: {product.Quantity}, Price: {product.Price}";
                productList.Add(s);
            }
            return Results.Ok(productList);
        });

      

        route.MapPost("/order", async (Sale sale) =>
        {
            // recalling OBJ channel and queue initiated at build time
            var channel = app.ServiceProvider.GetRequiredService<OrderChannel>();
            var queue = app.ServiceProvider.GetRequiredService<OrderQueue>();
            

            try
            {    
                // write order in channel
                await channel.Writer.WriteAsync(sale);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enqueuing order: {ex.Message}");
                return Results.Problem("Error processing order.");
            }

            return Results.Created($"/sales/{sale.ProductId}", sale);
        });

        return app;
    }
}


