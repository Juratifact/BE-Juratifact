namespace Juratifact.Service.User;

public interface IUserService
{
    public Task<string> CreateUser(Request.CreateUserRequest request);
    
    public Task<string> UpdateUser(Guid id, Request.UpdateUserRequest request);
    
    public Task<string> SoftDeleteUser(Guid id);
    
    public Task<Response.GetUserResponse>  GetUserProfile(Guid userId);
    public Task<Response.GetUserResponse>  GetUserByName(string userName);
    public Task<Base.Response.PageResult<Response.GetUserResponse>> GetAllUser(string? searchTerm, int pageIndex, int pageSize);
}