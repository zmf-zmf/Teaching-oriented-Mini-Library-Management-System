using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class BooksIntegrationTests : IClassFixture<AuthWebApplicationFactory>
    {
        private readonly AuthWebApplicationFactory _factory;

        public BooksIntegrationTests(AuthWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Books_Index_ReturnsSuccess_AndContainsSeededBook()
        {
            var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync("/Books");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("SeededOrderBook", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
