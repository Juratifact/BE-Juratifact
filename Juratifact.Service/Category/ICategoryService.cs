namespace Juratifact.Service.Category;

public interface ICategoryService
{
    public Task<List<Response.CategoryResponse>> GetCategories();
    
    public Task<List<Response.CategoryResponse>> GetCategoriesByParentId(Guid parentId);

    public Task<string> CreateCategory(Request.CreateCategoryRequest request);

    public Task<string> UpdateCategory(Guid categoryId, Request.UpdateCategoryRequest request);
    
    public Task<string> DeleteCategory(Guid categoryId);
}