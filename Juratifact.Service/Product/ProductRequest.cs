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
}