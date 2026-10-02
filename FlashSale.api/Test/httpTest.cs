
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FlashSale.Api.Test;

public sealed class TestOrderProcessing
{

      public TestOrderProcessing(){

      }
      private static readonly HttpClient Client = new()
      {
            BaseAddress = new Uri("http://localhost:5255")
      };

      [Fact]
      public async Task Concurrency_http_parallel_test()
      {
            const string user = "mapatel";
            const int submittedOrders = 100000;
            const int initialStock = 100000;
            using var resetResponse = await Client.PostAsJsonAsync(
                  $"/sales/admin/resetInventory?userId={user}",
                  new[]
                  {
                        new
                        {
                              id = 1,
                              name = "Concurrency test product",
                              category = "Test",
                              description = "Product used by the concurrency HTTP test",
                              price = 400,
                              quantity = initialStock,
                              specs = new Dictionary<string, string>()
                        }
                  });
            Assert.True(
                  resetResponse.IsSuccessStatusCode,
                  $"The API must be running at {Client.BaseAddress} before this test is executed.");

            var start = Stopwatch.GetTimestamp();
            var requests = Enumerable.Range(0, submittedOrders).Select(orderNumber =>
                  Client.PostAsJsonAsync("/sales/placeorder", new
                  {
                        userId = $"stress-user-{orderNumber}",
                        productId = 1,
                        quantity = 1,
                        price = 400
                  }));

            var responses = await Task.WhenAll(requests);
            var end = Stopwatch.GetElapsedTime(start);
         
            Console.WriteLine($"Total time processing: {end}");
            var accepted = responses.Count(response => response.StatusCode == HttpStatusCode.Accepted);
            var rejected = responses.Count(response => response.StatusCode == HttpStatusCode.Conflict);

            Assert.Equal(submittedOrders, accepted + rejected);
            Assert.Equal(submittedOrders, accepted);

            foreach (var response in responses)
            {
                  response.Dispose();
            }

            var expectedStock = initialStock - accepted;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            int stock;
            do
            {
                  await Task.Delay(100, timeout.Token);
                  stock = await ReadStockAsync(user, timeout.Token);
            }
            while (stock > expectedStock);

            Assert.Equal(expectedStock, stock);
      }

      private static async Task<int> ReadStockAsync(string user, CancellationToken cancellationToken)
      {
            using var response = await Client.GetAsync(
                  $"/sales/admin/liveinventory?userId={user}", cancellationToken);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(
                  await response.Content.ReadAsStringAsync(cancellationToken));

            return document.RootElement.EnumerateArray()
                  .Single(product => product.GetProperty("id").GetInt32() == 1)
                  .GetProperty("stock")
                  .GetInt32();
      }

}
