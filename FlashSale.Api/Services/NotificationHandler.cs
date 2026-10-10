



using System.Net.Http.Json;
using System.Text.Encodings.Web;
using OrderMessage.OrderEvent;
using Service.ServicesHandler;

namespace Services.NotificationHandler;



public class NotificationHandler : ServiceHandler
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public NotificationHandler(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async ValueTask HandleAsync(OrderEvent data, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Brevo:ApiKey"];
        var senderEmail = _configuration["Brevo:SenderEmail"];
        var senderName = _configuration["Brevo:SenderName"] ?? "FlashSale";
        var recipientEmail = "maharshi7178.p@gmail.com";

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(senderEmail) ||
            string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new InvalidOperationException(
                "Configure Brevo:ApiKey, Brevo:SenderEmail, and Brevo:RecipientEmail.");
        }

        var subject = $"FlashSale order {data._orderId}: {data._state}";
        var htmlContent = $"""
            <h2>Order status update</h2>
            <p>Order: {HtmlEncoder.Default.Encode(data._orderId.ToString())}</p>
            <p>Status: {HtmlEncoder.Default.Encode(data._state.ToString())}</p>
            <p>Product: {HtmlEncoder.Default.Encode(data._productId.ToString())}</p>
            <p>Quantity: {HtmlEncoder.Default.Encode(data._qty.ToString())}</p>
            <p>Total: {HtmlEncoder.Default.Encode(data._price.ToString("C"))}</p>
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = recipientEmail } },
            subject,
            htmlContent
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Brevo email request failed ({(int)response.StatusCode}): {responseBody}",
                null,
                response.StatusCode);
        }
    }
}