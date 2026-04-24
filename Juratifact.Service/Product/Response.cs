using Juratifact.Repository.Enum;
using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.Product;

public class Response
{
    public class ProductResponse : ProductMedia
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Condition { get; set; }
        public decimal? Price { get; set; }
        public ProductStatus Status { get; set; }
        // public string ImageUrl { get; set; }
        // public string VideoUrl { get; set; }
    }
    
    public class ProductMedia
    {
        public List<string> ImageUrl { get; set; }
        public List<string> Video { get; set; }
    }

    

    public class ProductCommentResponse
    {
        public Guid CommentId { get; set; }
        public Guid ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string Content { get; set; } //Comment
        public string? UserName { get; set; }
        public Guid? ParentCommentId { get; set; } // If this is a reply
    }
}