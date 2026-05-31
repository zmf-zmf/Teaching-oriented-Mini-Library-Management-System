using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmallShopSystem.Controllers;
using SmallShopSystem.Data;
using SmallShopSystem.Models;
using Xunit;
using System.Security.Claims;

namespace Tests
{
    public class HomeControllerUnitTests
    {
        [Fact]
        public void Index_AsWarehouse_SetsPendingAndLowStockCounts()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                var category = new Category { Name = "HC-Category" };
                var publisher = new Publisher { Name = "HC-Publisher", Contact = "Test", Phone = "000" };
                context.Categories.Add(category);
                context.Publishers.Add(publisher);
                context.SaveChanges();

                var lowStockBook = new Book { Title = "LowStock", ISBN = "L-001", Price = 1.0m, Stock = 3, CategoryId = category.Id, PublisherId = publisher.Id };
                var goodStockBook = new Book { Title = "GoodStock", ISBN = "G-001", Price = 2.0m, Stock = 20, CategoryId = category.Id, PublisherId = publisher.Id };
                context.Books.AddRange(lowStockBook, goodStockBook);
                context.SaveChanges();

                var customer = new Customer { Name = "C1", Email = "c1@test", Address = "addr" };
                context.Customers.Add(customer);
                context.SaveChanges();

                var order = new Order { OrderDate = DateTime.UtcNow, Status = "´ý·¢»õ", CustomerId = customer.Id, BookId = lowStockBook.Id, Quantity = 1 };
                context.Orders.Add(order);
                context.SaveChanges();

                var logger = new NullLogger<HomeController>();
                var controller = new HomeController(logger, context);

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Warehouse") }, "TestAuth"))
                    }
                };

                var result = controller.Index() as ViewResult;

                Assert.NotNull(result);
                var pending = result.ViewData["PendingOrders"] as int? ?? 0;
                var lowStock = result.ViewData["LowStockCount"] as int? ?? 0;

                Assert.Equal(1, pending);
                Assert.Equal(1, lowStock);
            }
        }
    }
}
