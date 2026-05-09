using Juratifact.Repository.Enum;

namespace Juratifact.Service.Report;

public class Response
{
    public class ReportResponse
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = "";
        public string? Description { get; set; }
        public ReportStatus Status { get; set; }
        public ProductDetailResponse Product { get; set; }
        public UserResponse Reporter { get; set; }
    }

    public class ProductDetailResponse
    {
        public Guid Id { get; set; }
        
        public UserResponse? Seller { get; set; }
        public string Title { get; set; }
        public string Condition { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
        public List<string> ImageUrl { get; set; } = new();
        public List<string?> Video { get; set; } = new();
        public List<CategoryResponse> ProductCategories { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }


    public class CategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
    }
}