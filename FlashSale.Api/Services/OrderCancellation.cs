using Disruptor;
using OrderMessage.OrderEvent;
using Inventory.InventorySchema;
using Microsoft.VisualBasic;
using Service.ServicesHandler;
using System.Diagnostics.Eventing.Reader;



public class OrderCancellation
{
    private readonly InventorySchema _inventorySchema;

    private readonly long _orderId; 

    public OrderCancellation(InventorySchema inventorySchema, long orderId)
    {
        _inventorySchema = inventorySchema;
        _orderId = orderId;
    }
    

    
    
}
