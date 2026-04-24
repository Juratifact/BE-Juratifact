namespace Juratifact.Service.Profile;

public interface IProfileService
{
    public Task<Response.ProfileResponse> GetUserById(Guid userId);
    public Task<Response.ProfileResponse> GetUserByUserName(string userName);
    
    public Task<Base.Response.PageResult<Response.ProfileResponse>> GetAllUser(string? searchTerm, int pageSize, int pageIndex);
}