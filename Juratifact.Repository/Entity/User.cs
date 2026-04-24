using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class User: BaseEntity<Guid>,IAuditableEntity
{
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    
    public string? UserName { get; set; } // ko bắt buộc
    public string? ProfilePicture { get; set; }
    public bool IsVerify { get; set; } = false;
    public int? VerifyCode { get; set; }
    
    public decimal TrustScore { get; set; } //
    public decimal TotalTrustScore { get; set; } //
    public int SellerReviewAmount { get; set; } //
    
    
    public Wallet Wallet { get; set; }
    
    public Cart Cart { get; set; }
    
    public ICollection<Order> Orders { get; set; } =  new List<Order>();
    public ICollection<UserRole> UserRoles { get; set; }  = new List<UserRole>();
    public ICollection<IdentityDocument> IdentityDocuments { get; set; }  = new List<IdentityDocument>();
    public ICollection<Report> Reports { get; set; }  = new List<Report>();
    public ICollection<ProductComment> ProductComments { get; set; } = new List<ProductComment>();
    public ICollection<UsePromotionSubscription>  UsePromotionSubscriptions { get; set; } = new List<UsePromotionSubscription>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}