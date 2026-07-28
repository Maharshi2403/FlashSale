using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace FlashSale.Api.Endpoints;
public static class SaleEndpoints
{
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var route = app.MapGroup("/sales");

        route.MapGet("/", () =>
        {
            return Results.Ok(new[]
            {
                "Laptop",
                "Phone",
                "Monitor"
            });
        });

        route.MapPost("/", (Sale sale) =>
        {
            // Save sale
            return Results.Created($"/sales/{sale.Id}", sale);
        });

        return app;
    }
}