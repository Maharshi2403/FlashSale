using System.Runtime.InteropServices;
using System.Threading.Channels;
using FlashSale.Api.OrderBook.Inventory;
using Microsoft.VisualBasic;

public class OrderProcessing
{
    Channel<Sale> _channel;
    Inventory inve;
    OrderQueue orderQueue;
    public OrderProcessing(Channel<Sale> ch, Inventory inn, OrderQueue orderQueue)
    {
        _channel = ch;
        inve = inn;
        this.orderQueue = orderQueue;
    }

    public async Task liveReader()
    {
        while(await _channel.Reader.WaitToReadAsync())
        {
            while(_channel.Reader.TryRead(out var order))
            {
                var qty = order.Quantity;

                if (qty > 0)
                {
                    inve.dic.TryGetValue(order.ProductId-1, out var product);
                    if (product != null)
                    {
                        if (product.Quantity >= qty)
                        {
                            product.Quantity -= qty;
                            orderQueue.Orders.Add(order);
                            Console.WriteLine($"Order processed: Product ID {order.ProductId}, Quantity {qty}, Total Price {order.TotalPrice}, User ID {order.userId}");
                        }
                        else
                        {
                            Console.WriteLine($"Insufficient quantity for Product ID {order.ProductId}. Available: {product.Quantity}, Requested: {qty}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Product ID {order.ProductId} not found in inventory.");
                    }
                }
            }
        }
    }
}