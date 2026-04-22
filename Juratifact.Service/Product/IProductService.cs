namespace Juratifact.Service.Product;

public interface IProductService
{
    public Task<string> CreateProduct(ProductRequest.CreateProductRequest request);
    
    public Task<ProductResponse.ProductCommentResponse> CreateComment(ProductRequest.ProductCommentRequest request);
    
    public Task<string> UpdateProductPostingById(Guid productId, ProductRequest.UpdateProductRequest request);
    
    public Task<string> SoftDeleteProductPostingById(Guid productId);
}