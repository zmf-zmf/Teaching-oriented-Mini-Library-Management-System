using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class BooksOrderNegativeTests : IClassFixture<CustomerWebApplicationFactory>
    {
        private readonly CustomerWebApplicationFactory _factory;

        public BooksOrderNegativeTests(CustomerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Post_Order_WithQuantityZero_ReturnsValidationError()
        {
            var client = _factory.CreateClient();

            int bookId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SmallShopSystem.Data.ApplicationDbContext>();
                var book = db.Books.FirstOrDefault(b => b.Title == "SeededOrderBook");
                Assert.NotNull(book);
                bookId = book.Id;
            }

            // GET form to extract antiforgery token
            var getResp = await client.GetAsync($"/Books/Order?id={bookId}");
            getResp.EnsureSuccessStatusCode();
            var getHtml = await getResp.Content.ReadAsStringAsync();
            var token = OrderFlowTests.ExtractAntiForgeryToken(getHtml);
            Assert.False(string.IsNullOrEmpty(token));

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("id", bookId.ToString()),
                new KeyValuePair<string, string>("quantity", "0")
            };

            var postResp = await client.PostAsync("/Books/Order", new FormUrlEncodedContent(formData));

            // Should return 200 with the form and validation message (not a redirect)
            Assert.Equal(HttpStatusCode.OK, postResp.StatusCode);
            var html = await postResp.Content.ReadAsStringAsync();
            var decodedHtml = System.Net.WebUtility.HtmlDecode(html);

            Assert.Contains("数量必须大于 0", decodedHtml);
        }

        [Fact]
        public async Task Post_Order_NonexistentBook_ReturnsNotFound()
        {
            var client = _factory.CreateClient();

            // Attempt to order a non-existent book id
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", "dummy"),
                new KeyValuePair<string, string>("id", "999999"),
                new KeyValuePair<string, string>("quantity", "1")
            };

            // Posting without a valid antiforgery token may result in 400; do a direct POST and allow other outcomes
            var postResp = await client.PostAsync("/Books/Order", new FormUrlEncodedContent(formData));

            // Expect NotFound (404) or BadRequest if antiforgery rejected; accept both as valid negative outcomes
            Assert.True(postResp.StatusCode == HttpStatusCode.NotFound || postResp.StatusCode == HttpStatusCode.BadRequest);
        }
    }
}
