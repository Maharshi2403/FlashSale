
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
            const int submittedOrders = 10000;
            const int initialStock = 10000;
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
                        userId = user,
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
         

            foreach (var response in responses)
            {
                  response.Dispose();
            }

           
      }


}
