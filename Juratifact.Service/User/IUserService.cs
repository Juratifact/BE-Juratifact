namespace Juratifact.Service.User;

public interface IUserService
{
    public Task<string> CreateUser(Request.CreateUserRequest request);
    
    public Task<string> UpdateUser(Guid id, Request.UpdateUserRequest request);
    
    public Task<string> SoftDeleteUser(Guid id);
}