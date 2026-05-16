using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace SmallShopSystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender, IMemoryCache memoryCache, ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "邮箱")]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
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
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var verificationCode = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        var cacheKey = GetCacheKey(Input.Email);
        _memoryCache.Set(cacheKey, verificationCode, TimeSpan.FromMinutes(10));

        var emailBody = $"<p>你的找回密码验证码是：<strong>{verificationCode}</strong></p><p>验证码有效期 10 分钟，请尽快返回网站输入。</p>";

        try
        {
            await _emailSender.SendEmailAsync(Input.Email, "找回密码验证码", emailBody);
            _logger.LogInformation("Verification code sent to {Email}", Input.Email);
        }
        catch
        {
            _memoryCache.Remove(cacheKey);
            throw;
        }

        return RedirectToPage("./ResetPassword", new { email = Input.Email });
    }

    private static string GetCacheKey(string email) => $"pwd-code:{email.Trim().ToLowerInvariant()}";
}
