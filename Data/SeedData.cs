// Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmallShopSystem.Models;

namespace SmallShopSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            string[] roles = new[] { "Admin", "Warehouse", "Support", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        loggerFactory?.CreateLogger("SeedData")?.LogWarning(
                            "Failed to create role {Role}: {Errors}",
                            role,
                            string.Join(", ", result.Errors.Select(e => e.Description)));
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

            await SyncCustomerRecordsFromIdentityAsync(userManager, context, loggerFactory);
            await SeedBookCatalogAsync(context, loggerFactory);
        }

        private static async Task SyncCustomerRecordsFromIdentityAsync(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            ILoggerFactory? loggerFactory)
        {
            var users = await userManager.Users.ToListAsync();
            if (users.Count == 0)
            {
                return;
            }

            var staffRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Admin",
                "Warehouse",
                "Support"
            };

            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                var isStaff = roles.Any(r => staffRoles.Contains(r));
                var isCustomer = roles.Any(r => string.Equals(r, "Customer", StringComparison.OrdinalIgnoreCase));

                if (isStaff && !isCustomer)
                {
                    continue;
                }

                var email = user.Email ?? user.UserName;
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var existingCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Email == email);
                if (existingCustomer != null)
                {
                    continue;
                }

                context.Customers.Add(new Customer
                {
                    Name = user.UserName ?? email,
                    Email = email,
                    Address = "待完善"
                });
            }

            await context.SaveChangesAsync();
            loggerFactory?.CreateLogger("SeedData")?.LogInformation("Synchronized customer records from Identity users.");
        }

        private static async Task SeedBookCatalogAsync(ApplicationDbContext context, ILoggerFactory? loggerFactory)
        {
            if (await context.Books.AnyAsync())
            {
                return;
            }

            var categoryNames = new[]
            {
                "文学",
                "历史",
                "科幻",
                "传记",
                "编程",
                "数据库",
                "网络",
                "管理",
                "心理",
                "教育"
            };

            var publisherSeeds = new[]
            {
                new { Name = "清华大学出版社", Contact = "王老师", Phone = "010-6278-1000" },
                new { Name = "人民邮电出版社", Contact = "李老师", Phone = "010-8105-5555" },
                new { Name = "机械工业出版社", Contact = "张老师", Phone = "010-8837-4444" },
                new { Name = "电子工业出版社", Contact = "赵老师", Phone = "010-8825-3333" },
                new { Name = "高等教育出版社", Contact = "刘老师", Phone = "010-5858-2222" },
                new { Name = "中信出版社", Contact = "陈老师", Phone = "010-8484-1111" },
                new { Name = "商务印书馆", Contact = "周老师", Phone = "010-6512-6666" },
                new { Name = "浙江人民出版社", Contact = "孙老师", Phone = "0571-8517-7777" },
                new { Name = "中国人民大学出版社", Contact = "吴老师", Phone = "010-6251-8888" },
                new { Name = "上海交通大学出版社", Contact = "郑老师", Phone = "021-6293-9999" }
            };

            var createdCategories = new List<Category>();
            foreach (var name in categoryNames)
            {
                var existing = await context.Categories.FirstOrDefaultAsync(c => c.Name == name);
                if (existing == null)
                {
                    existing = new Category { Name = name };
                    context.Categories.Add(existing);
                }

                createdCategories.Add(existing);
            }

            var createdPublishers = new List<Publisher>();
            foreach (var seed in publisherSeeds)
            {
                var existing = await context.Publishers.FirstOrDefaultAsync(p => p.Name == seed.Name);
                if (existing == null)
                {
                    existing = new Publisher
                    {
                        Name = seed.Name,
                        Contact = seed.Contact,
                        Phone = seed.Phone
                    };
                    context.Publishers.Add(existing);
                }

                createdPublishers.Add(existing);
            }

            await context.SaveChangesAsync();

            var topicMap = new Dictionary<string, string[]>
            {
                ["文学"] = new[] { "经典导读", "现代散文", "短篇小说", "诗词鉴赏", "名著选读", "文学写作", "世界名篇", "中国当代文学", "外国文学", "阅读训练" },
                ["历史"] = new[] { "中国通史", "世界通史", "近代史纲要", "古代文明", "历史人物", "史学方法", "帝国兴衰", "战争与和平", "文明演进", "历史地图" },
                ["科幻"] = new[] { "银河远航", "星际边界", "未来纪元", "人工智能纪事", "量子之门", "深空探索", "末日回声", "火星计划", "机械觉醒", "宇宙漫游" },
                ["传记"] = new[] { "科学巨匠", "创业人生", "艺术大师", "思想者", "时代人物", "榜样力量", "名人故事", "成长记录", "自传精选", "人生轨迹" },
                ["编程"] = new[] { "C#入门", "ASP.NET Core实战", "JavaScript进阶", "算法基础", "面向对象设计", "Git版本控制", "RESTful API开发", "前端工程化", "软件工程", "架构设计" },
                ["数据库"] = new[] { "SQL Server基础", "MySQL应用", "数据库系统原理", "索引优化", "事务与锁", "EF Core实战", "建模设计", "性能调优", "备份恢复", "数据仓库" },
                ["网络"] = new[] { "计算机网络基础", "TCP/IP详解", "路由交换", "网络安全", "Linux网络", "Web协议", "云网络", "网络诊断", "无线通信", "企业网络" },
                ["管理"] = new[] { "项目管理", "团队协作", "运营策略", "时间管理", "领导力", "流程优化", "组织行为学", "目标管理", "商业分析", "决策方法" },
                ["心理"] = new[] { "情绪管理", "沟通心理学", "积极心理学", "人际关系", "压力应对", "认知行为", "自我成长", "亲密关系", "心理咨询入门", "习惯养成" },
                ["教育"] = new[] { "教学设计", "班级管理", "学习方法", "教育心理学", "课程开发", "课堂评价", "家庭教育", "儿童发展", "阅读指导", "教育技术" }
            };

            var books = new List<Book>();
            var sequence = 1;
            foreach (var category in createdCategories)
            {
                if (!topicMap.TryGetValue(category.Name, out var topics))
                {
                    continue;
                }

                for (var i = 0; i < topics.Length; i++)
                {
                    var publisher = createdPublishers[(sequence + i) % createdPublishers.Count];
                    var price = Math.Round(18.0m + category.Id * 1.8m + i * 2.6m, 2);
                    var stock = 6 + ((sequence + i) * 7 % 48);

                    books.Add(new Book
                    {
                        Title = $"{category.Name}{topics[i]}",
                        ISBN = $"9787{category.Id:00}{sequence + i:000000}",
                        Price = price,
                        Stock = stock,
                        CategoryId = category.Id,
                        PublisherId = publisher.Id
                    });
                }

                sequence += topics.Length;
            }

            await context.Books.AddRangeAsync(books);
            await context.SaveChangesAsync();

            loggerFactory?.CreateLogger("SeedData")?.LogInformation(
                "Seeded {CategoryCount} categories, {PublisherCount} publishers and {BookCount} books.",
                createdCategories.Count,
                createdPublishers.Count,
                books.Count);
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
