namespace Juratifact.Service.Product;

public class ProductResponse
{
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