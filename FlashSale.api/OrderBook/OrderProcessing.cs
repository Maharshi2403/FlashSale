using System.Runtime.InteropServices;
using System.Threading.Channels;
using FlashSale.Api.OrderBook.Inventory;
using Microsoft.VisualBasic;

public class OrderProcessing
{
    Channel<Sale> _channel;
    Inventory inve;
    public OrderProcessing(Channel<Sale> ch, Inventory inn)
    {
        _channel = ch;
        inve = inn;
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
                    inve.dic.TryGetValue(order.ProductId, out var product);
                    if (product != null)
                    {
                        if (product.Quantity >= qty)
                        {
                            product.Quantity -= qty;
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