using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.IdentityDocumentService;

public class Request
{
    public class UploadIdentityDocumentRequest
    {
        public string IdCardFrontUrl { get; set; }
        public string IdCardBackUrl { get; set; }  
    }
    
    public class ReUploadIdentityDocumentRequest
    {
        public Guid DocumentId { get; set; }
        public string IdCardFrontUrl { get; set; }
        public string IdCardBackUrl { get; set; }  
    }


    

 


}