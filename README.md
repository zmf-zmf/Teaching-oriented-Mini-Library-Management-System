# 小型网店管理系统（SmallShopSystem）

本项目为课程设计：基于 B/S 架构的在线书店管理系统，使用 `.NET 8` 与 Razor Pages 开发，数据访问使用 EF Core，默认数据库为 SQL Server（LocalDB）。系统实现了图书/分类/出版社管理、客户账户与个人中心、订单处理与发货、权限控制与邮件通知等功能，便于演示与教师验收。

快速运行（本机演示）

1. 恢复依赖：`dotnet restore`
2. 运行项目：`dotnet run --project SmallShopSystem.csproj --urls http://0.0.0.0:5201`
3. 打开浏览器访问：`http://localhost:5201` 或使用本机 IP 进行局域网访问
<img width="2559" height="1316" alt="image" src="https://github.com/user-attachments/assets/981c03fe-a60c-4bd5-9c17-e07896d5413d" />


自动化测试

- 运行全部测试：`dotnet test Tests/Tests.csproj`
- 测试包括图书展示、下单流程、负面用例、鉴权与邮件服务等模块（使用内存数据库进行隔离测试）。

说明

- 项目已包含种子数据，启动后可直接用于演示。默认测试账号与密码配置在项目配置文件中，便于教师验收。
- 如需切换到生产数据库或第三方邮件/支付接口，请参考代码中的配置项并按需修改。

仓库结构（简要）

- 应用主程序：`SmallShopSystem` 项目
- 测试工程：`Tests` 项目
