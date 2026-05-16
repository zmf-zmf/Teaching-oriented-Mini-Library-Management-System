using Microsoft.AspNetCore.Mvc;
using SmallShopSystem.Models;
using SmallShopSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace SmallShopSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            // 待发货订单
            ViewBag.PendingOrders = _context.Orders
                .Count(o => o.Status == "待发货");

            // 库存预警 (阈值 10)
            ViewBag.LowStockCount = _context.Books
                .Count(b => b.Stock < 10);

            ViewBag.RecentPendingOrders = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Book)
                .Where(o => o.Status == "待发货")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            ViewBag.FeaturedBooks = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .OrderBy(b => b.Id)
                .Take(6)
                .ToList();

            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}