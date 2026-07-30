using System.Threading.Channels;
using FlashSale.Api.OrderBook.Inventory;
namespace FlashSale.Api.OrderBook.OrderChannel;
public class OrderChannel
{
    private readonly Channel<Sale> channel;
    
    public ChannelWriter<Sale> Writer => channel.Writer;

    public ChannelReader<Sale> Reader => channel.Reader;
    public OrderChannel()
    {
        // Initialize the order channel

        channel = Channel.CreateUnbounded<Sale>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                

            }
        );
        
    }
}