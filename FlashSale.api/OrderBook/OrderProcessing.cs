using System.Diagnostics;
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
                var stopwatch = Stopwatch.StartNew();
                var qty = order.Quantity;

                try
                {
                    if (qty > 0)
                    {
                        inve.dic.TryGetValue(order.ProductId-1, out var product);
                        if (product != null)
                        {
                            if (product.Quantity >= qty)
                            {
                                product.Quantity -= qty;
                                order.ProcessingTimeNanoseconds = (long)(stopwatch.ElapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency);
                                order.ProcessingTimeMicroseconds = stopwatch.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency;
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
                finally
                {
                    stopwatch.Stop();
                    var elapsedUs = stopwatch.ElapsedTicks * 1_000_000.0 / Stopwatch.Frequency;
                    var elapsedNs = stopwatch.ElapsedTicks * 1_000_000_000.0 / Stopwatch.Frequency;
                    Console.WriteLine($"Order timing: Product ID {order.ProductId}, User ID {order.userId}, elapsed {elapsedNs:F0} ns ({elapsedUs:F3} us)");
                }
            }
        }
    }
}