using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.User;

public class Request
{
    public class CreateUserRequest
    {
        public required string Email { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public required string FullName { get; set; }
        public required string Password { get; set; }
        public required string PhoneNumber { get; set; }
        public string UserName { get; set; }
        
        
    }
}