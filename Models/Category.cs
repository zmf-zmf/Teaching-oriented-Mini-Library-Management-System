// Models/Category.cs
using System.Collections.Generic;

namespace SmallShopSystem.Models
{
    public class Category
    {
        public int Id { get; set; } // 主键
        public string Name { get; set; } = null!; // 类别名称：如“小说”、“技术”

        // 导航属性：一个类别下有多本书
        public List<Book>? Books { get; set; }
    }
}