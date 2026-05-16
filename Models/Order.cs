// Models/Order.cs
namespace SmallShopSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } // 下单时间
        public string Status { get; set; } = "待发货"; // 订单状态

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}

// Models/Customer.cs
namespace YourProjectName.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; } // 收货地址
    }
}