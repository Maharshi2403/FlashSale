using Disruptor;
using Inventory.InventorySchema;
using OrderMessage.OrderEvent;



public  class InventoryReservationHandler: IEventHandler<OrderEvent>
{
    
    private InventorySchema inventory;
    public InventoryReservationHandler(InventorySchema inventorySchema)
    {
        inventory = inventorySchema;
    }

    public async void OnEvent(OrderEvent data, long sequence, bool endOfBatch)
    {
        var token = inventory.TryReserv(data._productId, data._qty);

        if (token is not null)
        {
            data._reservation = token;
            data._state = OrderState.RESERVED;

            return;
        }


        data._state = OrderState.FAILED;



    }

}