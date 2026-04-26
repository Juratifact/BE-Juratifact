using Juratifact.Repository.Enum;

namespace Juratifact.Service.IdentityDocumentService;

public class Response
{

    public class IdentityDocumentResponse
    {
        public Guid   Id             { get; set; }
        public string IdCardFrontUrl { get; set; }
        public string IdCardBackUrl  { get; set; }
        public string SelfieUrl { get; set; }
        public IdentityStatus Status { get; set; }
        public string? Note          { get; set; } 
        public DateTimeOffset? VerifiedAt { get; set; }
        public DateTimeOffset CreatedAt   { get; set; }
    }

    public class UserSummary
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public string Email { get; set; }
    }
    

    public class IdentityDocumentAdminResponse : IdentityDocumentResponse
    {
        public UserSummary User { get; set; }
    }

   
}