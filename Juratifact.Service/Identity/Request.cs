using System.ComponentModel.DataAnnotations;

namespace Juratifact.Service.Identity;

public class Request
{
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}