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
        public Repository.Entity.Product Product { get; set; }
        public Repository.Entity.User Reporter { get; set; }
    }

    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = "";
        public string? Description { get; set; }
        
        public ProductListResponse Product  { get; set; }
        public UserReport User { get; set; }
        
    }

    public class ProductListResponse
    {
        public Guid ProductId { get; set; }
        public Guid SellerId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Condition { get; set; }
        public decimal? Price { get; set; }
        public List<string> ImageUrl { get; set; }
        public List<string> Video { get; set; }
    }

    public class UserReport
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        
    }
}