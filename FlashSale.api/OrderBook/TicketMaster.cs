namespace FlashSale.Api.OrderBook.TicketMaster;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;

public class TicketMaster
{
    private readonly Channel<OrderEventMessage> snow_ch;
    private readonly HttpClient httpClient;

    public TicketMaster(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();

        snow_ch = Channel.CreateBounded<OrderEventMessage>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });
    }

    public async Task PublishOrder(OrderEventMessage ovm)
    {
        try
        {
            await snow_ch.Writer.WriteAsync(ovm);
        }
        catch
        {
            Console.WriteLine("Order write failed to snow channel");
        }

        await PublishTicket(ovm.OrderId);
    }

    public async Task PublishTicket(long orderId)
    {
        while (await snow_ch.Reader.WaitToReadAsync())
        {
            while (snow_ch.Reader.TryRead(out var ovm))
            {
                if (ovm.OrderId == orderId)
                {
                    await SendOrderToSnow(ovm);
                }
            }
        }
    }

   public async Task<HttpResponseMessage> SendOrderToSnow(OrderEventMessage order)
{
    Console.WriteLine("SNOW request came in");

    var snowApiUrl = "https://kinaxis.service-now.com/api/now/table/sc_task";
    
    var userToken = "def8524e93530710e8fab8566aba10d56f5479a3db27e75ee455112d7ff35cae7cca4caf";
    
    if (string.IsNullOrWhiteSpace(userToken))
    {
        throw new InvalidOperationException(
            "SNOW_USER_TOKEN environment variable is required.");
    }

    var requestBody = new
    {
        short_description = $"FlashSale order #{order.OrderId} completed",
        description = $"""
            FlashSale order details:
            Order ID: {order.OrderId}
            User ID: {order.UserId}
            Product ID: {order.ProductId}
            Quantity: {order.Quantity}
            Unit Price: {order.Price}
            Total Price: {order.Price * order.Quantity}
            Reservation Token: {order.ReservationToken}
            Order State: {order.State}
            """,
        category = "inquiry",
        impact = "3",
        urgency = "3"
    };

    var bodyJson = JsonSerializer.Serialize(requestBody);

    using var request = new HttpRequestMessage(HttpMethod.Post, snowApiUrl)
    {
        Content = new StringContent(bodyJson, Encoding.UTF8, "application/json")
    };

    // Use X-UserToken instead of Basic Auth
    request.Headers.Add("X-UserToken", userToken);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    try
    {
        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"SNOW response: {(int)response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine($"SNOW response body: {responseBody}");

        return response;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SNOW request failed: {ex.Message}");
        throw;
    }
}
}