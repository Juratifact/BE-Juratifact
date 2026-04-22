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
}