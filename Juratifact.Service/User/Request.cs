using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.User;

public class Request
{
    public class CreateUserRequest
    {
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string Password { get; set; }
        public required string PhoneNumber { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        // Deprecated: free-text address. Prefer sending VietMapRefId instead.
        public string? Address { get; set; }
        // Optional: VietMap reference id. If provided, service will resolve place and store vietmap fields for the user.
        public string? VietMapRefId { get; set; }
        public string? UserName { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
    
    
    public class CreateShipperRequest
    {
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string Password { get; set; }
        public required string PhoneNumber { get; set; }
    }
    
}