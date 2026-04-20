using Juratifact.Repository.Abstraction;

namespace Juratifact.Repository.Entity;

public class User: BaseEntity<Guid>,IAuditableEntity
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string HashedPassword { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public string Salt { get; set; }
    public bool isVerify { get; set; } = false;
    public decimal TrustScore { get; set; } = 0;
    
    
    public Wallet Wallet { get; set; }
    public Cart Cart { get; set; }
    
    public ICollection<Order> Orders { get; set; } =  new List<Order>();
    public ICollection<UserRole> UserRoles { get; set; }  = new List<UserRole>();
    public ICollection<Otp>  Otps { get; set; }  = new List<Otp>();
    public ICollection<IdentityDocument> IdentityDocuments { get; set; }  = new List<IdentityDocument>();
    public ICollection<Report> Reports { get; set; }  = new List<Report>();
    public ICollection<SellerReview> SellerReviews { get; set; }  = new List<SellerReview>();
    
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}