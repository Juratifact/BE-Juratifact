using Juratifact.Repository;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Category;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Response.CategoryResponse>> GetCategories()
    {
        var query = _dbContext.Categories.Where(x => true);
        
        query = query.OrderBy(x => x.Name);

        var selected = query.Select(x => new Response.CategoryResponse()
        {
            Id = x.Id,
            Name = x.Name,
            ParentId = x.ParentId
        });
        
        var listResult = await selected.ToListAsync();
        return listResult;
    }

    public async Task<List<Response.CategoryResponse>> GetCategoriesByParentId(Guid parentId)
    {
        var query = _dbContext.Categories.Where(x => x.ParentId == parentId);
        
        query = query.OrderBy(x => x.Name);

        var selected = query.Select(x => new Response.CategoryResponse()
        {
            Id = x.Id,
            Name = x.Name,
            ParentId = x.ParentId
        });

        var listResult = await selected.ToListAsync();
        return listResult;
    }

    public async Task<string> CreateCategory(Request.CreateCategoryRequest request)
    {
        var existingCategoryQuery = _dbContext.Categories
            .Where(x => x.Name == request.Name);

        bool isExistingCategory = await existingCategoryQuery.AnyAsync();

        if (isExistingCategory)
        {
            throw new Exception("Name category already exists");
        }
        
        // CHỈ kiểm tra danh mục cha nếu request có chứa ParentId (Tức là đang tạo danh mục con)
        if (request.ParentId != null) 
        {
            var existingParentCategoryQuery = _dbContext.Categories.Where(x => x.Id == request.ParentId);

            bool isExistingParentCategory = await existingParentCategoryQuery.AnyAsync();

            if (!isExistingParentCategory)
            {
                throw new Exception("Parent category not found");
            }
        }
        
        var category = new Repository.Entity.Category()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ParentId = request.ParentId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Add(category);
        await _dbContext.SaveChangesAsync();
        
        return "Category created";
    }

    public async Task<string> UpdateCategory(Guid categoryId, Request.UpdateCategoryRequest request)
    {
        var query = _dbContext.Categories.Where(x => x.Id == categoryId);

        var category = await query.FirstOrDefaultAsync();

        if (category == null)
        {
            throw new Exception("Category not found");
        }

        if (category.Name != request.Name)
        {
            bool isNameTaken = await _dbContext.Categories.AnyAsync(x => x.Name == request.Name);
            if (isNameTaken)
            {
                throw new Exception("Name category already exists");
            }
            
            category.Name = request.Name;
            category.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await  _dbContext.SaveChangesAsync();
        return "Category updated";
    }

    public async Task<string> DeleteCategory(Guid categoryId)
    {
        var query = _dbContext.Categories.Where(x => x.Id == categoryId && x.IsDeleted == false);

        var category = await query.FirstOrDefaultAsync();

        if (category == null)
        {
            throw new Exception("Category not found");
        }

        bool hasChildren = await query.AnyAsync();

        if (hasChildren)
        {
            throw new Exception("Cannot delete a category that contains sub-categories");
        }
        
        await _dbContext.SaveChangesAsync();
        return "Category deleted";
    }
}