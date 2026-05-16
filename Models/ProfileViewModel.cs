namespace SmallShopSystem.Models;

public class ProfileViewModel
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();

    public string Nickname { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public string DeletePassword { get; set; } = string.Empty;
    public string DeleteConfirmText { get; set; } = string.Empty;

    public IList<OrderSummaryViewModel> RecentOrders { get; set; } = new List<OrderSummaryViewModel>();
}

public class OrderSummaryViewModel
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}
