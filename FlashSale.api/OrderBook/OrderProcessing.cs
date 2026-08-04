using System.Runtime.InteropServices;
using System.Threading.Channels;
using FlashSale.Api.OrderBook.Inventory;
using Microsoft.VisualBasic;

public class OrderProcessing
{
    Channel<Sale> _channel;
    Inventory inve;
    public OrderProcessing(Channel<Sale> ch, Inventory inn)
    {
        _channel = ch;
        inve = inn;
    }

    public async Task liveReader()
    {
        while(await _channel.Reader.WaitToReadAsync())
        {
            while(_channel.Reader.TryRead(out var order))
            {
                var qty = order.Quantity;

                if()
            }
        }
    }
}