using Juratifact.Repository.Enum;
using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.Product;

public class Response
{
    public class ProductResponse : ProductMedia
    {
        public Guid ProductId { get; set; }
        public Guid SellerId { get; set; }
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

    public class ProductCommentResponseFull : ProductResponse
    {
        public List<string> Comments { get; set; }
        public List<string> Replies { get; set; }
    }
    
    
    // fix response
    
    public class UserInfo
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
    }
    
    public class ReplyDto
    {
        public Guid CommentId { get; set; }
        public Guid? ParentCommentId { get; set; }

        public string Content { get; set; } = null!;
        public UserInfo User { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }
    
    public class CommentDto
    {
        public Guid CommentId { get; set; }
        public Guid? ParentCommentId { get; set; }

        public string Content { get; set; } = null!;
        public UserInfo User { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }

        public int ReplyCount { get; set; }
        // public bool HasMoreReplies { get; set; }
        // public string? NextCursor { get; set; }

        public List<ReplyDto> Replies { get; set; } = new();
    }
    
    public class ProductCommentsResponse : ProductResponse
    {
        public List<CommentDto> Comments { get; set; } = new();
    }
    
}