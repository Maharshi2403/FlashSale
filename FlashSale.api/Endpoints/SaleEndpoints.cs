using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FlashSale.Api.OrderBook.InventoryManager;
using FlashSale.Api.OrderBook;
using FlashSale.Api.Hubs;
using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Reflection.Metadata.Ecma335;
using FlashSale.Api.OrderBook;

namespace FlashSale.Api.Endpoints;

public record PlaceOrderRequest(string UserId, int ProductId, int Quantity, decimal Price);


//Middelware for Filter Admin changes
public  sealed class AdminFilter: IEndpointFilter
{
     public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

       
        var userId = httpContext.Request.Query["userId"].ToString();

        if (string.IsNullOrWhiteSpace(userId) ||
            !SaleEndpoints.admin_validator(userId))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}

    



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
    public static readonly List<string> Admins = ["mapatel", "gkhan"];
  
    public static bool admin_validator(string userId)
    {
        foreach(string s in Admins)
        {
            if(s.Equals(userId.ToLower()))
            {
                return true;
            } 
        }
        return false;
    }


    public static bool add_new_admin(string userId)
    {
        bool val_alpha = Regex.IsMatch(userId, @"^[a-zA-Z]+$");

        if (!val_alpha)
        {
            Admins.Add(userId);
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var route = app.MapGroup("/sales");
        var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();


//************************** ADMIN ROUTS *****************************************//    
        
           // Manually update inventory from CSV file, this endpoint can be used to refresh the inventory without restarting the application.
        // !! Danger, need to test will it affect ongoing orders if inventory is updated while orders are being processed.

        // good solution - this route should pause POST/order request untile invenotry gets updated 
         route.MapPost("/resetInventory",(List<Product> product, string userId) =>
        {   
            
            inventory.PopulateInventory(product);
            return Results.Ok(inventory.Products);
        }).AddEndpointFilter<AdminFilter>();

        route.MapPost("/updateinventory", (List<Product> product, string userId) =>
        {
            
        }).AddEndpointFilter<AdminFilter>();
            
        
         // will display inventory for admin page
        route.MapGet("/items", (string userId) =>
        {
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            var productList = inventory.Products.Select(product => new
            {
                id = product.Id,
                name = product.Name,
                category = product.Category,
                description = product.Description,
                price = product.Price,
                specs = product.Specs
            }).ToList();

            return Results.Ok(productList);
        }).AddEndpointFilter<AdminFilter>();


//************************** ADMIN ROUTS *****************************************// 


       
        route.MapGet("/userorder", (string userId) =>
        {
            var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            OrderEventMessage[] orders;
            Console.Write("order view request came in");

            
            
                orders = inventory._orders.TryGetValue(userId, out var userOrders)
                    ? userOrders.ToArray()
                    : Array.Empty<OrderEventMessage>();
            

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

       
         
     
        route.MapPost("/order", (PlaceOrderRequest order) =>


        {
            // // Restrict user for ordering same product again
            //  var inventory = app.ServiceProvider.GetRequiredService<DisruptorEngine>().GetInventory();
            // OrderEventMessage[] orders;

            // lock (inventory._orders)
            // {
            //     orders = inventory._orders.TryGetValue(order.UserId, out var userOrders)
            //         ? userOrders.ToArray()
            //         : Array.Empty<OrderEventMessage>();
            // }

            // if(orders.Any(o => o.ProductId == order.ProductId))
            // {
            //     Console.WriteLine("User Aready have an existing order in this catagory");
            //     return Results.Conflict(new
            //     {
            //         accepted = false,
            //         message = "User Aready have an existing order in this catagory"
            //     });
            // }

            if (inventory.GetStock(order.ProductId) > 0)
            {
                    var confirm = app.ServiceProvider.GetRequiredService<DisruptorEngine>().PublishOrder(order.UserId, order.ProductId, order.Quantity, order.Price);
                    return Results.Accepted($"/sales/orders/{confirm}", new { confirm });
            }
            else
            {
                return Results.Conflict(new
                {
                    accepted = false,
                    message = "Out of Stock Item"
                });
            }

            
            
        });

        route.MapDelete("/cancelorder", (long orderId, string userId, int productId) =>
        {
            
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


