
using OrderMessage.OrderEvent;


namespace Service.ServicesHandler;


public interface ServiceHandler
{
    ValueTask HandleAsync(
        OrderEvent data,
        CancellationToken cancellationToken
    );
}




