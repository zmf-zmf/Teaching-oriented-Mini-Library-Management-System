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

            string[] roles = new[] { "Admin", "Warehouse", "Support", "Customer" };

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

            var defaultUsers = new[]
            {
                new { Role = "Admin", Email = configuration["AdminUser:Email"] ?? "admin@smallshop.local", Password = configuration["AdminUser:Password"] ?? "Admin123!" },
                new { Role = "Warehouse", Email = configuration["WarehouseUser:Email"] ?? "warehouse@smallshop.local", Password = configuration["WarehouseUser:Password"] ?? "Warehouse123!" },
                new { Role = "Support", Email = configuration["SupportUser:Email"] ?? "support@smallshop.local", Password = configuration["SupportUser:Password"] ?? "Support123!" },
                new { Role = "Customer", Email = configuration["CustomerUser:Email"] ?? "customer@smallshop.local", Password = configuration["CustomerUser:Password"] ?? "Customer123!" }
            };

            foreach (var account in defaultUsers)
            {
                await EnsureUserInRoleAsync(userManager, loggerFactory, account.Email, account.Password, account.Role);
            }
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<IdentityUser> userManager,
            ILoggerFactory? loggerFactory,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    loggerFactory?.CreateLogger("SeedData")?.LogWarning(
                        "Failed to create {Role} user: {Errors}",
                        role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addResult = await userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    loggerFactory?.CreateLogger("SeedData")?.LogWarning(
                        "Failed to add {Email} to role {Role}: {Errors}",
                        email,
                        role,
                        string.Join(", ", addResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}