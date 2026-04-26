using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.IdentityDocumentService;

public class Request
{
    public class UploadIdentityDocumentRequest
    {
        public IFormFile IdCardFrontUrl { get; set; }
        public IFormFile IdCardBackUrl { get; set; }  
        public IFormFile SelfieUrl { get; set; }
    }
    
    public class ReUploadIdentityDocumentRequest: UploadIdentityDocumentRequest
    {
        public Guid DocumentId { get; set; }
    }


    

 


}