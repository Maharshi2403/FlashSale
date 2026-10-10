using Disruptor;
using Inventory.InventorySchema;
using OrderChanel.ServiceChannel;
using OrderMessage.OrderEvent;



public  class InventoryReservationHandler: IEventHandler<OrderEvent>
{
    
    private InventorySchema _inventory;
    private ServiceChannel _channel;
    public InventoryReservationHandler(InventorySchema inventorySchema, ServiceChannel channel )
    {
        _inventory = inventorySchema;

        _channel = channel;

    }

    public  void OnEvent(OrderEvent data,long sequence, bool endOfBatch)
    {
        var token = _inventory.TryReserv(data._productId, data._qty);

        if (token is not null)
        {
            data._reservation = token;
            data._state = OrderState.RESERVED;
            // Fire addinal services and forget move to next order
            if (!_channel.Writer.TryWrite(data))
            {
                Console.WriteLine("Channel is Full");
            }
            
            return;
        }


        data._state = OrderState.FAILED;
        


    }

}

