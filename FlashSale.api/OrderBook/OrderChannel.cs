// using System.Threading.Channels;
// using FlashSale.Api.OrderBook.InventoryManager;
// using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;
// namespace FlashSale.Api.OrderBook.OrderChannel;
// public class OrderChannel
// {
//     public Channel<OrderEventMessage> channel;
    
//     public ChannelWriter<OrderEventMessage> Writer => channel.Writer;

//     public ChannelReader<OrderEventMessage> Reader => channel.Reader;
//     public OrderChannel()
//     {
//         // Initialize the order channel
//         channel = Channel.CreateUnbounded<OrderEventMessage>(
//             new UnboundedChannelOptions
//             {
//                 SingleReader = true,
//                 SingleWriter = false,
                

//             }
//         );
        
//     }
// }