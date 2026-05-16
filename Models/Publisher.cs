namespace SmallShopSystem.Models
{
    public class Publisher
    {
        public int Id { get; set; }

        public string Name { get; set; } // 出版商名称，如“清华大学出版社”

        public string? Contact { get; set; } // 联系人姓名

        public string? Phone { get; set; } // 联系电话

        // 导航属性：一个出版商可以出版很多本书
        public List<Book>? Books { get; set; }
    }
}