using Microsoft.AspNetCore.Http.HttpResults;
using OrderEngine.DisruptorEngine;

namespace Endpoints;

public sealed record CreateOrderRequest(string? UserId, int ProductId, int Quantity, decimal Price);

public sealed record OrderAcceptedResponse(long OrderId, string Status);

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/orders",
                Results<Accepted<OrderAcceptedResponse>, BadRequest<string>> (
                    CreateOrderRequest request,
                    DisruptorEngine engine) =>
                {
                    if (string.IsNullOrWhiteSpace(request.UserId) ||
                        request.ProductId <= 0 ||
                        request.Quantity <= 0 ||
                        request.Price < 0)
                    {
                        return TypedResults.BadRequest(
                            "UserId is required; ProductId and Quantity must be positive; Price cannot be negative.");
                    }

                    var orderId = engine.PublishOrder(
                        request.UserId,
                        request.ProductId,
                        request.Quantity,
                        request.Price);

                    return TypedResults.Accepted(
                        uri: (string?)null,
                        value: new OrderAcceptedResponse(orderId, "INITIALIZED"));
                })
            .WithTags("Orders")
            .WithName("CreateOrder")
            .WithSummary("Submit an order for processing")
            .WithDescription("Publishes the order to the order engine and returns its generated ID.");
    }
}