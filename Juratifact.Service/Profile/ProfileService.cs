using Juratifact.Repository;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Profile;

public class ProfileService: IProfileService
{
    private readonly AppDbContext _dbContext;
    public ProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Response.ProfileResponse> GetUserById(Guid userId)
    {
        var query = _dbContext.Users.Where (x => x.Id == userId);

        if (query == null)
        {
            throw new ArgumentException("User not found");
        }
    
        var selectedQuery = query.Select(x => new Response.ProfileResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
        });
        var result = await selectedQuery.FirstOrDefaultAsync();
        
        return result!;
    }

    public async Task<Response.ProfileResponse> GetUserByUserName(string userName)
    {
        
        var query = _dbContext.Users.Where (x => x.UserName == userName);

        if (query == null)
        {
            throw new ArgumentException("User not found");
        }
    
        var selectedQuery = query.Select(x => new Response.ProfileResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
        });
        var result = await selectedQuery.FirstOrDefaultAsync();
        
        return result!;
    }

    public async Task<Base.Response.PageResult<Response.ProfileResponse>> GetAllUser(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.Users.Where(x => true);
        if (searchTerm != null)
        {
            query = query.Where(x => x.UserName!.Contains(searchTerm) ||
                                     x.Email.Contains(searchTerm) ||
                                     x.PhoneNumber.Contains(searchTerm));
        }
        
        query = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var selectedQuery = query.Select(x => new Response.ProfileResponse()
        {
            UserName = x.UserName!,
            FullName = x.FullName,
            Email = x.Email,
            PhoneNumber = x.PhoneNumber,
            Address = x.Address!,
            ProfilePicture = x.ProfilePicture!,
        });
        
        var listResult = await selectedQuery.ToListAsync();
        var totalItems = listResult.Count;
 
        var result = new Base.Response.PageResult<Response.ProfileResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
 
    }
}