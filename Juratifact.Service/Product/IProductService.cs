namespace Juratifact.Service.Product;

public interface IProductService
{
    public Task<string> CreateProduct(ProductRequest.CreateProductRequest request);
    
    public Task<ProductResponse.ProductCommentResponse> CreateComment(ProductRequest.ProductCommentRequest request);
}