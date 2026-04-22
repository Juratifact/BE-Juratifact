using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Juratifact.Service.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Product;

public class ProductService: IProductService
{
    private readonly AppDbContext _dbContext;


    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
  
    }

    public async Task<Base.Response.PageResult<Response.ProductRespone>> GetAll(int pageSize, int pageIndex)
     {
         var query = _dbContext.Products.Where(x => x.Status == ProductStatus.Available);
         
         query = query.Skip((pageIndex - 1) * pageSize)
             .Take(pageSize);
         var selected = query.Select(x => new Response.ProductRespone()
         {
             Title = x.Title,
             Description = x.Description,
             Price = x.Price,
             Status = x.Status,
             Condition = x.Condition,
         });
         var listResult = await selected.ToListAsync();
         var totalItems = listResult.Count;
 
         var result = new Base.Response.PageResult<Response.ProductRespone>()
         {
             Items = listResult,
             PageIndex = pageIndex,
             PageSize = pageSize,
             TotalItems = totalItems,
         };
         return result;
 
     }

    public async Task<Base.Response.PageResult<Response.ProductRespone>> GetByTitle(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Products.Where(x => x.Status == ProductStatus.Available);

        if (searchTerm != null)
        {
            query = query.Where(x => x.Title.Contains(searchTerm));
        }
        query = query.OrderBy(x => x.Title);
        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        var selected = query.Select(x => new Response.ProductRespone()
        {
            Title = x.Title,
            Description = x.Description,
            Price = x.Price,
            Status = x.Status,
            Condition = x.Condition,
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;
 
        var result = new Base.Response.PageResult<Response.ProductRespone>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
 
    }

    public async Task<Base.Response.PageResult<Response.ProductRespone>> GetByCondition(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Products.Where(x => x.Status == ProductStatus.Available);

        if (searchTerm != null)
        {
            query = query.Where(x => x.Condition.Contains(searchTerm));
        }
        query = query.OrderBy(x => x.Condition);
        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        var selected = query.Select(x => new Response.ProductRespone()
        {
            Title = x.Title,
            Description = x.Description,
            Price = x.Price,
            
            Status = x.Status,
            Condition = x.Condition,
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;
 
        var result = new Base.Response.PageResult<Response.ProductRespone>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }

    
}