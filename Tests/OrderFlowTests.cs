using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Tests
{
    // Authentication handler that returns a fixed test customer principal
    internal class TestCustomerAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestCustomerAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Use a fixed id that the factory seeds into the Identity store
            var userId = "test-customer-id";
            var email = "customer@test.local";
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, "Customer")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class CustomerWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with InMemory
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Use a unique in-memory database name per factory instance to avoid cross-test pollution
                var customerDbName = Guid.NewGuid().ToString();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(customerDbName));

                // Register test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestCustomerAuthHandler>("Test", options => { });

                // Ensure DB is created and seed an example book
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();

                    // Seed Identity user in AspNetUsers via UserManager
                    var userManager = scopedServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                    var roleManager = scopedServices.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

                    var userId = "test-customer-id";
                    var email = "customer@test.local";

                    // Ensure role exists
                    if (!roleManager.Roles.Any(r => r.Name == "Customer"))
                    {
                        roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Customer")).GetAwaiter().GetResult();
                    }

                    var existingUser = userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
                    if (existingUser == null)
                    {
                        var user = new Microsoft.AspNetCore.Identity.IdentityUser
                        {
                            Id = userId,
                            UserName = email,
                            Email = email,
                            EmailConfirmed = true
                        };
                        var res = userManager.CreateAsync(user, "Password123!").GetAwaiter().GetResult();
                        if (res.Succeeded)
                        {
                            userManager.AddToRoleAsync(user, "Customer").GetAwaiter().GetResult();
                        }
                    }

                    // Seed minimal book data if missing
                    if (!db.Books.Any())
                    {
                        var category = new SmallShopSystem.Models.Category { Name = "OrderCategory" };
                        var publisher = new SmallShopSystem.Models.Publisher { Name = "OrderPublisher", Contact = "Test", Phone = "000" };
                        db.Categories.Add(category);
                        db.Publishers.Add(publisher);
                        db.SaveChanges();

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
                        db.SaveChanges();
                    }
                }
            });

            base.ConfigureWebHost(builder);
        }
    }

    public class OrderFlowTests : IClassFixture<CustomerWebApplicationFactory>
    {
        private readonly CustomerWebApplicationFactory _factory;

        public OrderFlowTests(CustomerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Customer_Can_Post_Order_And_See_OrderSuccess()
        {
            var client = _factory.CreateClient();

            // Get the seeded book id
            int bookId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var book = db.Books.FirstOrDefault(b => b.Title == "SeededOrderBook");
                Assert.NotNull(book);
                bookId = book.Id;
            }

            // GET the order form to obtain antiforgery token
            var getResp = await client.GetAsync($"/Books/Order?id={bookId}");
            getResp.EnsureSuccessStatusCode();
            var getHtml = await getResp.Content.ReadAsStringAsync();

            // extract the antiforgery token from the form
            var token = ExtractAntiForgeryToken(getHtml);
            Assert.False(string.IsNullOrEmpty(token));

            // Prepare the form content including the antiforgery token and fields
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("id", bookId.ToString()),
                new KeyValuePair<string, string>("quantity", "2")
            };

            var postResp = await client.PostAsync("/Books/Order", new FormUrlEncodedContent(formData));

            // Follow redirect to OrderSuccess
            postResp.EnsureSuccessStatusCode();
            var html = await postResp.Content.ReadAsStringAsync();

            Assert.Contains("SeededOrderBook", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("2", html);

            // Verify the order was persisted in the test database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var orderInDb = db.Orders.Include(o => o.Book).Include(o => o.Customer).FirstOrDefault(o => o.Book.Title == "SeededOrderBook" && o.Quantity == 2);
                Assert.NotNull(orderInDb);
            }
        }

        public static string ExtractAntiForgeryToken(string html)
        {
            var match = System.Text.RegularExpressions.Regex.Match(html, @"name=""__RequestVerificationToken""[^>]*?value=""([^""]+)""");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }
    }
}
