using System.Threading.Channels;
using Disruptor;
using OrderMessage.OrderEvent;

namespace OrderChanel.ServiceChannel;


public class ServiceChannel
{
    private readonly Channel<OrderEvent> _channel;

    public ChannelWriter<OrderEvent> Writer { get; }

    public ChannelReader<OrderEvent> Reader { get; }

    public ServiceChannel()
    {
        _channel = Channel.CreateUnbounded<OrderEvent>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            }
        );

        Writer = _channel.Writer;
        Reader = _channel.Reader;
    }


}


