namespace Juratifact.Service.User;

public class Response
{
    public class GetUserResponse
    {
        public string? UserName { get; set; }
        public string?FullName { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal? TrustScore { get; set; }
        public string? ProfilePicture { get; set; }
        
    }
}