using Juratifact.Repository.Enum;

namespace Juratifact.Service.Product;

public class Response
{
    public class ProductRespone
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Condition { get; set; }
        public decimal? Price { get; set; }
        public ProductStatus Status { get; set; }
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