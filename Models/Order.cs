using System;

namespace SmallShopSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } // 下单时间
        public string Status { get; set; } = "待发货"; // 订单状态

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int? BookId { get; set; }
        public Book? Book { get; set; }

        public int Quantity { get; set; } = 1;
    }
}