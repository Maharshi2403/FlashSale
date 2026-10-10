using Inventory.InventorySchema;
using Disruptor.Dsl;
using OrderMessage.OrderEvent;
using Disruptor;
using OrderChanel.ServiceChannel;
using System.Reflection.Metadata.Ecma335;

namespace OrderEngine.DisruptorEngine;



public class DisruptorEngine
{
    private readonly Disruptor<OrderEvent> _disruptor;

    private readonly RingBuffer<OrderEvent> _ringbuffer;

    private readonly InventorySchema _inventoryShema;

    private readonly ServiceChannel _channel;
    
    private long _orderId;

    public DisruptorEngine(InventorySchema inventoryshema, ServiceChannel channel,int bufferSize)
    {
        _inventoryShema = inventoryshema;
        _channel = channel;

        var dslDisruptor = new Disruptor<OrderEvent>(
          () => new OrderEvent(),
          bufferSize,
          TaskScheduler.Default,
          ProducerType.Multi,
          new BusySpinWaitStrategy()
        );


        dslDisruptor
        .HandleEventsWith(new InventoryReservationHandler(_inventoryShema, _channel));


        _disruptor = dslDisruptor;
        _ringbuffer = _disruptor.RingBuffer;
     
    }


    public void Start()
    {
        _disruptor.Start();
    }

    

    public long PublishOrder(string userId, int productId, int quantity, decimal price)
    {
        long orderId = Interlocked.Increment(ref _orderId);

        long sequence = _ringbuffer.Next();

        try
        {
            var orderEvent = _ringbuffer[sequence];
            orderEvent._orderId = orderId;
            orderEvent._userId = userId;
            orderEvent._productId = productId;
            orderEvent._qty = quantity;
            orderEvent._price = price;
            orderEvent._timeStamp = System.Diagnostics.Stopwatch.GetTimestamp();
            orderEvent._state = OrderState.INITIALIZED;
            orderEvent._reservation = null;
            
        }
        finally
        {
            _ringbuffer.Publish(sequence);
        }

        return orderId;
    }



}
