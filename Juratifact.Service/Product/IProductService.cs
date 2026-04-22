namespace Juratifact.Service.Product;

public interface IProductService
{
    public Task<string> CreateProduct(Request.CreateProductRequest request);
    
    public Task<Response.ProductCommentResponse> CreateComment(Request.ProductCommentRequest request);
    
    public Task<string> UpdateProductPostingById(Guid productId, Request.UpdateProductRequest request);
    
    public Task<string> SoftDeleteProductPostingById(Guid productId);
}