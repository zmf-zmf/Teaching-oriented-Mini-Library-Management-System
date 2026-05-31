using System.Net;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallShopSystem.Data;
using Xunit;

namespace Tests
{
    // Test authentication handler that issues an authenticated principal with the Admin role
    internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class AuthWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with InMemory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Use a unique in-memory database name per factory instance to avoid cross-test pollution
                var authDbName = Guid.NewGuid().ToString();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(authDbName));

                // Add test authentication scheme
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Ensure DB is created and seed minimal data for assertions
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();

                    if (!db.Orders.Any())
                    {
                        // Seed category and publisher
                        var category = new SmallShopSystem.Models.Category { Name = "SeedCategory" };
                        var publisher = new SmallShopSystem.Models.Publisher { Name = "SeedPublisher", Contact = "Seed", Phone = "000" };
                        db.Categories.Add(category);
                        db.Publishers.Add(publisher);
                        db.SaveChanges();

                        // Seed a book and customer
                        var book = new SmallShopSystem.Models.Book
                        {
                            Title = "SeededOrderBook",
                            ISBN = "000-SEED-000",
                            Price = 5.00m,
                            Stock = 10,
                            CategoryId = category.Id,
                            PublisherId = publisher.Id
                        };
                        db.Books.Add(book);

                        var customer = new SmallShopSystem.Models.Customer { Name = "SeedCustomer", Email = "seed@local", Address = "addr" };
                        db.Customers.Add(customer);
                        db.SaveChanges();

                        // Seed an order
                        var order = new SmallShopSystem.Models.Order
                        {
                            OrderDate = DateTime.UtcNow,
                            Status = "´ý·¢»õ",
                            CustomerId = customer.Id,
                            BookId = book.Id,
                            Quantity = 2
                        };
                        db.Orders.Add(order);
                        db.SaveChanges();
                    }
                }
            });

            base.ConfigureWebHost(builder);
        }
    }

    public class ProtectedPageTests : IClassFixture<AuthWebApplicationFactory>
    {
        private readonly AuthWebApplicationFactory _factory;

        public ProtectedPageTests(AuthWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Orders_Index_WithAdminRole_ReturnsSuccess_AndContainsSeededOrder()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // OrdersController is protected with [Authorize(Roles = "Admin,Warehouse,Support")]
            var response = await client.GetAsync("/Orders");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("SeededOrderBook", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SeedCustomer", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
