using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.Product;

public class ProductRequest
{
    public class CreateProductRequest
    {
        public required string Title { get; set; }
        public required string Condition { get; set; }
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public required string Status { get; set; }
        public IFormFile? Image { get; set; }
        public IFormFile? Video { get; set; }
    }

    public class ProductCommentRequest
    {
        public required Guid ProductId { get; set; }
        public required string Content { get; set; }
        public Guid? ParentCommentId { get; set; } // For replies/answers
        
        //Logic: Parent1(question): 
        //          Parent2(reply to question): ParentCommentId = Parent1.CommentId
        //          //          Parent3(reply to Parent2): ParentCommentId = Parent2.CommentId
    }
}