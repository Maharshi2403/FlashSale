
namespace FlashSale.Api.OrderBook.OrderEvent;
public class OrderEvent{
    
     // 64-byte cache line 1
    public long OrderId;
    public long UserId;
    public int ProductId;
    public int Quantity;
    public decimal Price;
    public long Timestamp;
    public OrderState State;
    
    // 64-byte cache line 2 (padding to prevent false sharing)
    public string? ReservationToken;
   
    public int InventoryReserved;
    
    // using 8 byte x 7 padding to make sure treads does not do false sharing
    private long _pad0, _pad1, _pad2, _pad3, _pad4, _pad5, _pad6;

    public OrderEvent(){
        
    }
    public OrderEvent(long orderId, long userId, int productId, int qty, decimal price)
    {
        OrderId = orderId;
        UserId = userId;
        ProductId = productId;
        Quantity = qty;
        Price = price;
        Timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        State = OrderState.PENDING;
        ReservationToken = null;
        InventoryReserved = 0;
        _pad0 = _pad1 = _pad2 = _pad3 = _pad4 = _pad5 = _pad6 = 0;
    }
}

public enum OrderState
{
    PENDING = 0,
    VALIDATED = 1,
    INVENTORY_RESERVED = 2,
    COMPLETED = 3,
    FAILED = -1
}
public struct OrderFaildEvent
{
    public long OrderId;

    public string Reason;


    public long Timestamp;
}