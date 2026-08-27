using Disruptor;
using Disruptor.Dsl;
using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;
using FlashSale.Api.OrderBook.InventoryManager;
using  InventoryManager = FlashSale.Api.OrderBook.InventoryManager.InventoryManager;
using FlashSale.Api.OrderBook.OrderEvent;
using FlashSale.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

public class DisruptorEngine
{
    public readonly Disruptor<OrderEventMessage> _disruptor;
    
    public readonly RingBuffer<OrderEventMessage> _ringBuffer; 


    private readonly InventoryManager _inventory;
    private long _orderIdSequence = 0;
    private readonly CompletionHandler _completionHandler;
    
    public DisruptorEngine(string contentRootPath, IHubContext<InventoryHub> hubContext, int bufferSize = 4096)
    {
        _inventory = new InventoryManager(contentRootPath);
        _inventory.PopulateInventory();
  
        // Create Disruptor with single producer
        var dslDisruptor = new Disruptor<OrderEventMessage>(
            () => new OrderEventMessage(),
            bufferSize,
            TaskScheduler.Default,
            ProducerType.Single,
            new BusySpinWaitStrategy()
        );
        
        // Wire up handler chain
        dslDisruptor
            .HandleEventsWith(new OrderValidationHandler())
            .Then(new InventoryReservationHandler(_inventory, hubContext))
            .Then(_completionHandler = new CompletionHandler(_inventory));
                        
        _disruptor = dslDisruptor;
        _ringBuffer = _disruptor.RingBuffer;
    }

    
    /// <summary>
    /// Start the Disruptor processing.
    /// </summary>
    public void Start()
    {
        _disruptor.Start();
        Console.WriteLine("✓ Disruptor started");
    }
    
    /// <summary>
    /// Publish order to ring buffer (non-blocking).
    /// </summary>
    public long PublishOrder(long userId, int productId, int quantity, decimal price)
    {
        var orderId = Interlocked.Increment(ref _orderIdSequence);
        
        long sequence = _ringBuffer.Next();
        try
        {
            var orderEvent = _ringBuffer[sequence];
            orderEvent.OrderId = orderId;
            orderEvent.UserId = userId;
            orderEvent.ProductId = productId;
            orderEvent.Quantity = quantity;
            orderEvent.Price = price;
            orderEvent.Timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            orderEvent.State = OrderState.PENDING;
            orderEvent.ReservationToken = null;
            orderEvent.InventoryReserved = 0;
           
        }
        finally
        {
            _ringBuffer.Publish(sequence);
        }
        Console.WriteLine("Order published");
        return orderId;
    }
    
    /// <summary>
    /// Shutdown Disruptor gracefully.
    /// </summary>
    public void Shutdown()
    {
        _disruptor.Shutdown();
        Console.WriteLine($"✓ Disruptor shutdown. Success: {_completionHandler.GetSuccessCount()}, Failed: {_completionHandler.GetFailureCount()}");
    }
    
    public InventoryManager GetInventory() => _inventory;






}