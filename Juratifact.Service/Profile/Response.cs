namespace Juratifact.Service.Profile;

public class Response
{
    public class ProfileResponse
    {
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string ProfilePicture { get; set; } = "";
    }
}