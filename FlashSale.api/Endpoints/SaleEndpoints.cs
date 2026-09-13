using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FlashSale.Api.OrderBook.InventoryManager;
using FlashSale.Api.OrderBook;
using FlashSale.Api.Hubs;
using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;

namespace FlashSale.Api.Endpoints;

public record PlaceOrderRequest(string UserId, int ProductId, int Quantity, decimal Price);



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

        route.MapGet("/orderview", (string userId) =>
        {
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            OrderEventMessage[] orders;
            Console.Write("order view request came in");

            lock (inventory._orders)
            {
                orders = inventory._orders.TryGetValue(userId, out var userOrders)
                    ? userOrders.ToArray()
                    : Array.Empty<OrderEventMessage>();
            }

            return Results.Ok(orders.Select(order => new
            {
                orderId = order.OrderId,
                userId = order.UserId,
                productId = order.ProductId,
                quantity = order.Quantity,
                price = order.Price,
                state = order.State,
                reservationToken = order.ReservationToken,
                inventoryReserved = order.InventoryReserved,
                timestamp = order.Timestamp
            }));

        });

       
         
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
            // Restrict user for ordering same product again
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            OrderEventMessage[] orders;

            lock (inventory._orders)
            {
                orders = inventory._orders.TryGetValue(order.UserId, out var userOrders)
                    ? userOrders.ToArray()
                    : Array.Empty<OrderEventMessage>();
            }

            if(orders.Any(o => o.ProductId == order.ProductId))
            {
                Console.WriteLine("User Aready have an existing order in this catagory");
                return Results.Conflict(new
                {
                    accepted = false,
                    message = "User Aready have an existing order in this catagory"
                });
            }



            var confirm = app.ServiceProvider.GetRequiredService<DisruptorEngine>().PublishOrder(order.UserId, order.ProductId, order.Quantity, order.Price);
            
            return Results.Accepted($"/sales/orders/{confirm}", new { confirm });
        });

        route.MapDelete("/cancelorder", (long orderId, string userId, int productId) =>
        {
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            OrderEventMessage? orderToCancel = null;
            Console.WriteLine("Order Cancelation request came in");
            lock (inventory._orders)
            {
                if (inventory._orders.TryGetValue(userId, out var userOrders))
                {
                    orderToCancel = userOrders.FirstOrDefault(order => order.OrderId == orderId);
                    if (orderToCancel?.State == FlashSale.Api.OrderBook.OrderEvent.OrderState.COMPLETED)
                        userOrders.Remove(orderToCancel);
                        
                        }
            }

            if (orderToCancel == null)
                return Results.NotFound(new { message = "Order not found." });

            if (orderToCancel.State != FlashSale.Api.OrderBook.OrderEvent.OrderState.COMPLETED)
                return Results.BadRequest(new { message = "Only completed orders can be cancelled." });

            inventory.Release(orderToCancel.ProductId, orderToCancel.InventoryReserved);
            inventory.PublishUpadate(productId);
            return Results.Ok(new { orderId, state = "CANCELLED" });
        });

        return app;
    }
}


