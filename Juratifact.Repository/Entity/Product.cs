using Juratifact.Repository.Abstraction;
using Juratifact.Repository.Enum;

namespace Juratifact.Repository.Entity;

public class Product : BaseEntity<Guid>, IAuditableEntity
{
    public Guid SellerId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public ProductStatus Status { get; set; } // trạng thái tin đăng lên
    public string Condition { get; set; } // New, Used, Refurbished, etc.
    
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
    public ICollection<ProductMedia> ProductMedias { get; set; } = new List<ProductMedia>();
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}