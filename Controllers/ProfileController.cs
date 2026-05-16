using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallShopSystem.Data;
using SmallShopSystem.Models;

namespace SmallShopSystem.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await BuildProfileViewModelAsync();
        if (model == null)
        {
            return Challenge();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var email = user.Email ?? user.UserName;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Challenge();
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer == null)
        {
            customer = new Customer
            {
                Name = string.IsNullOrWhiteSpace(model.Nickname) ? email : model.Nickname.Trim(),
                Email = email,
                Address = string.IsNullOrWhiteSpace(model.Address) ? "待完善" : model.Address.Trim()
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer.Name = string.IsNullOrWhiteSpace(model.Nickname) ? email : model.Nickname.Trim();
            customer.Address = string.IsNullOrWhiteSpace(model.Address) ? "待完善" : model.Address.Trim();
        }

        await _context.SaveChangesAsync();
        TempData["ProfileMessage"] = "个人资料已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword) || string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
        {
            ModelState.AddModelError(string.Empty, "请完整填写密码信息。");
            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.CurrentPassword = model.CurrentPassword;
            vm.NewPassword = model.NewPassword;
            vm.ConfirmNewPassword = model.ConfirmNewPassword;
            return View("Index", vm);
        }

        if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "两次新密码输入不一致。");
            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.CurrentPassword = model.CurrentPassword;
            vm.NewPassword = model.NewPassword;
            vm.ConfirmNewPassword = model.ConfirmNewPassword;
            return View("Index", vm);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.CurrentPassword = model.CurrentPassword;
            vm.NewPassword = model.NewPassword;
            vm.ConfirmNewPassword = model.ConfirmNewPassword;
            return View("Index", vm);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["ProfileMessage"] = "密码已修改。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (string.IsNullOrWhiteSpace(model.DeletePassword) || !string.Equals(model.DeleteConfirmText?.Trim(), "注销", StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "请输入正确的密码并输入“注销”确认。");
            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.DeletePassword = model.DeletePassword;
            vm.DeleteConfirmText = model.DeleteConfirmText;
            return View("Index", vm);
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, model.DeletePassword);
        if (!passwordOk)
        {
            ModelState.AddModelError(string.Empty, "密码错误，无法注销账号。");
            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.DeletePassword = model.DeletePassword;
            vm.DeleteConfirmText = model.DeleteConfirmText;
            return View("Index", vm);
        }

        var email = user.Email ?? user.UserName;
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            foreach (var error in deleteResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var vm = await BuildProfileViewModelAsync();
            if (vm == null) return Challenge();
            vm.DeletePassword = model.DeletePassword;
            vm.DeleteConfirmText = model.DeleteConfirmText;
            return View("Index", vm);
        }

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User account {Email} was deleted.", email);
        return RedirectToAction("Index", "Home");
    }

    private async Task<ProfileViewModel?> BuildProfileViewModelAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return null;
        }

        var email = user.Email ?? user.UserName ?? string.Empty;
        var roles = await _userManager.GetRolesAsync(user);
        var customer = await _context.Customers
            .Include(c => c.Orders)
            .ThenInclude(o => o.Book)
            .FirstOrDefaultAsync(c => c.Email == email);

        if (customer == null && !string.IsNullOrWhiteSpace(email))
        {
            customer = new Customer
            {
                Name = user.UserName ?? email,
                Email = email,
                Address = "待完善"
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        var recentOrders = customer?.Orders?
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new OrderSummaryViewModel
            {
                Id = o.Id,
                BookTitle = o.Book?.Title ?? "未知图书",
                Quantity = o.Quantity,
                Status = o.Status,
                OrderDate = o.OrderDate,
                TotalAmount = (o.Book?.Price ?? 0m) * o.Quantity
            })
            .ToList() ?? new List<OrderSummaryViewModel>();

        return new ProfileViewModel
        {
            Email = email,
            UserName = user.UserName ?? email,
            Roles = roles,
            Nickname = customer?.Name ?? user.UserName ?? email,
            Address = customer?.Address ?? string.Empty,
            RecentOrders = recentOrders
        };
    }
}
