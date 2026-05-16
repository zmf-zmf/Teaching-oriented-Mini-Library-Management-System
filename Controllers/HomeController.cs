using Microsoft.AspNetCore.Mvc;
using SmallShopSystem.Models;
using SmallShopSystem.Data;
using System.Diagnostics;

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

        public IActionResult Index()
        {
            // 1. 获取待发货订单数
            ViewBag.PendingOrders = _context.Orders
                .Count(o => o.Status == "待发货");

            // 2. 获取库存预警数 (少于 10 本的书籍)
            ViewBag.LowStockCount = _context.Books
                .Count(b => b.Stock < 10);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}