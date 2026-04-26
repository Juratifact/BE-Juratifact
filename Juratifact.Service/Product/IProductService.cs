namespace Juratifact.Service.Product;

public interface IProductService
{
    public Task<Base.Response.PageResult<Response.ProductResponse>> GetAll(
        int pageSize,
        int pageIndex);
    public Task<Base.Response.PageResult<Response.ProductResponse>> GetByTitle(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    public Task<Base.Response.PageResult<Response.ProductResponse>> GetByCondition(
        string? searchTerm,
        int pageSize,
        int pageIndex);
    
    public Task<Response.ProductCommentResponseFull> GetCommentsByProductId(Guid productId);
    
    public Task<string> CreateProduct(Request.CreateProductRequest request);
    
    public Task<Response.ProductCommentResponse> CreateComment(Request.ProductCommentRequest request);
    
    public Task<string> UpdateProductPostingById(Guid productId, Request.UpdateProductRequest request);
    
    public Task<string> SoftDeleteProductPostingById(Guid productId);
    
    
    public Task<Base.Response.PageResult<Response.ProductResponse>> GetByPrice(
        decimal? searchTerm,
        int pageSize,
        int pageIndex);
    
    // public Task<Base.Response.PageResult<Response.ProductResponse>> GetCategory(
    //     string? searchTerm,
    //     int pageSize,
    //     int pageIndex);
    
}