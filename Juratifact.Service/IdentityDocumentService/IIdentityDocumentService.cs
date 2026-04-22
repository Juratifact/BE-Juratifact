using Juratifact.Repository.Enum;

namespace Juratifact.Service.IdentityDocumentService;

public interface IIdentityDocumentService
{
    public Task<string> SubmitIdentityDocumentAsync(Request.UploadIdentityDocumentRequest request);
    public Task<string> ReSubmitIdentityDocumentAsync(Request.ReUploadIdentityDocumentRequest request);
    public Task<Response.IdentityDocumentResponse> GetMyDocumentAsync();
    public Task<Base.Response.PageResult<Response.IdentityDocumentAdminResponse>> GetAllAsync(IdentityStatus? status, int pageIndex, int pageSize);
    public Task<Response.IdentityDocumentAdminResponse> GetByIdAsync(Guid documentId);
    public Task<string> ApproveAsync(Guid documentId);
    public Task<string> RejectAsync(Guid documentId, string reason);
}