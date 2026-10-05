using System.Net.Sockets;

namespace OrderMessage.OrderEvent;


public enum OrderState
{

    //API accepted order validating user rules
    INITIALIZED = 0,

    //Ring buffer accepted and reserved inventory
    RESERVED = 1,

    // Services published order in database logs, and integrated platforms
    PUBLISHED = 2,
    
    // Failed 
    FAILED = 3,

    // Cancelled
    CANCELLED = 4
}

public class OrderEvent
{
    public  long _orderId;

    public required string _userId;

    public int _productId;

    public int _qty;

    public long _timeStamp;

    public required string _reservation;

    public OrderState _state;

    private long _pad0, _pad1, _pad2, _pad3, _pad4, _pad5, _pad6;

    //Empty Constructor
    public OrderEvent()
    {
        
    }


    public OrderEvent(int productId ,int qty, string userId, string reservation,OrderState state, long orderId, long timeStamp)
    { 
        _orderId = orderId;
        _userId = userId;
        _productId = productId;
        _qty = qty;
        _reservation = reservation;
        _timeStamp = timeStamp;
        _state = state;
        _pad0 = _pad1 = _pad2 = _pad3 = _pad4 = _pad5 = _pad6 = 0;


    }

}