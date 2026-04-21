namespace Juratifact.Service.User;

public interface IUserService
{
    public Task<string> CreateUser(UserRequest.CreateUserRequest request);
}