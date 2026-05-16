namespace SmallShopSystem.Models
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; } // 客户姓名

        public string? Email { get; set; } // 邮箱

        public string Address { get; set; } // 收货地址

        // 导航属性：一个客户可以有多个订单
        public List<Order>? Orders { get; set; }
    }
}