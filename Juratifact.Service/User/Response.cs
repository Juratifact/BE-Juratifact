namespace Juratifact.Service.User;

public class Response
{
    public class GetUserResponse
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string?FullName { get; set; }
       
        public string? Address { get; set; }
        // VietMap address fields
        public string? VietMapRefId { get; set; }
        public string? VietMapDisplay { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal? TrustScore { get; set; }
        public string? ProfilePicture { get; set; }
        
    }

    public class GetMyRoleResponse
    {
        public List<UserRoles>? UserRoles { get; set; }
    }

    public class UserRoles
    {
        public required string RoleName { get; set; }
    }
}