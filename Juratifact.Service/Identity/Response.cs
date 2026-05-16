namespace Juratifact.Service.Identity;

public class Response
{
    public class IdentityResponse
    {
        public string Access_token { get; set; } = null!;
        public Guid UserId { get; set; }
        public bool IsVerify { get; set; }
        public List<string> Roles { get; set; } = new();

    }
}
