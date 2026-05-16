// Data/SeedData.cs
using Microsoft.AspNetCore.Identity;

namespace SmallShopSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            string[] roles = new[] { "Admin", "Warehouse", "Support" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var r = new IdentityRole(role);
                    var res = await roleManager.CreateAsync(r);
                    if (!res.Succeeded)
                    {
                        loggerFactory?.CreateLogger("SeedData")?.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", res.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 从配置读取默认管理员账号（仅用于开发）
            var adminEmail = configuration["AdminUser:Email"] ?? "admin@smallshop.local";
            var adminPassword = configuration["AdminUser:Password"] ?? "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    loggerFactory?.CreateLogger("SeedData")?.LogWarning("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}