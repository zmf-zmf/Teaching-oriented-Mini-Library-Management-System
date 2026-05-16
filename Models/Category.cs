// Models/Category.cs
namespace SmallShopSystem.Models
{
    public class Category
    {
        public int Id { get; set; } // 主键
        public string Name { get; set; } // 类别名称：如“小说”、“技术”

        // 导航属性：一个类别下有多本书
        public List<Book>? Books { get; set; }
    }
}

// Models/Publisher.cs
namespace YourProjectName.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; } // 出版商名称
        public string? Contact { get; set; } // 联系人
    }
}