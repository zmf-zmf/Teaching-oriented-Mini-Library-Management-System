using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace SmallShopSystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMemoryCache _memoryCache;

    public ResetPasswordModel(UserManager<IdentityUser> userManager, IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _memoryCache = memoryCache;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "验证码必须是5位数字。")]
        [Display(Name = "验证码")]
        public string VerificationCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "密码至少 {2} 位。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare(nameof(Password), ErrorMessage = "两次密码输入不一致。")]
        public string ConfirmPassword { get; set; } = string.Empty;

    }

    public IActionResult OnGet(string? email = null)
    {
        if (email == null)
        {
            return BadRequest("必须提供邮箱。");
        }

        Input = new InputModel
        {
            Email = email
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var cacheKey = GetCacheKey(Input.Email);
        if (!_memoryCache.TryGetValue(cacheKey, out string? storedCode) || string.IsNullOrWhiteSpace(storedCode))
        {
            ModelState.AddModelError(string.Empty, "验证码已过期或不存在，请重新发送验证码。");
            return Page();
        }

        if (!string.Equals(storedCode, Input.VerificationCode, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "验证码错误，请重新输入。");
            return Page();
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, Input.Password);
        if (result.Succeeded)
        {
            _memoryCache.Remove(cacheKey);
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private static string GetCacheKey(string email) => $"pwd-code:{email.Trim().ToLowerInvariant()}";
}
