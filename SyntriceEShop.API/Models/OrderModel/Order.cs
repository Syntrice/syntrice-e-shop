using SyntriceEShop.API.Models.OrderProductModel;
using SyntriceEShop.API.Models.ProductModel;
using SyntriceEShop.API.Models.UserModel;

namespace SyntriceEShop.API.Models.OrderModel;

public class Order : IEntity<int>
{
    public int Id { get; set; }
    public int UserId { get; set; } 
    public decimal TotalPrice { get; set; }
    public DateTime CreatedOnUTC { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public IEnumerable<Product> Products { get; } = [];
    public IEnumerable<OrderProduct> OrderProducts { get; } = [];
}