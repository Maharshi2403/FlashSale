using Microsoft.AspNetCore.SignalR;

namespace FlashSale.Api.Hubs;

public class InventoryHub : Hub
{
}

public record StockUpdate(int ProductId, int Stock);
