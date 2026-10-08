using Disruptor;
using OrderMessage.OrderEvent;
using Inventory.InventorySchema;



public class OrderCancellation: IEventHandler<OrderEvent>
{
    
    public void OnEvent(OrderEvent data, long sequence, bool endOfBatch)
    {
        
    }


}