using System.Threading.Channels;
using OrderMessage.OrderEvent;
using Service.ServicesHandler;

namespace OrderChanel.ServiceChannel;


public class ServiceChannel
{
    private readonly Channel<OrderEvent> _channel;

    public ChannelWriter<OrderEvent> Writer { get; }

    public ChannelReader<OrderEvent> Reader { get; }

    public ServiceChannel(IEnumerable<ServiceHandler> handlers, ILogger<ServiceChannel> logger)
    {
        _channel = Channel.CreateUnbounded<OrderEvent>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true,
                AllowSynchronousContinuations = true
            }
        );

        Writer = _channel.Writer;
        Reader = _channel.Reader;
        _ = ProcessHandlersAsync(handlers, logger, CancellationToken.None);

    }

   

    private async Task ProcessHandlersAsync(
        IEnumerable<ServiceHandler> handlers,
        ILogger<ServiceChannel> logger,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await foreach (var order in Reader.ReadAllAsync(cancellationToken))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(order, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception,
                        "Order handler {HandlerType} failed for order {OrderId}",
                        handler.GetType().Name,
                        order._orderId);
                }
            }
        }
    }

}




