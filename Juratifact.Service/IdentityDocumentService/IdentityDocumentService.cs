using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Juratifact.Service.MediaService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.IdentityDocumentService;

public class IdentityDocumentService : IIdentityDocumentService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IMediaService _mediaService;

    public IdentityDocumentService(AppDbContext dbContext,  IHttpContextAccessor httpContext,  IMediaService mediaService)
    {
        _dbContext = dbContext;
        _httpContext = httpContext;
        _mediaService = mediaService;
    }
        
    public async Task<string> SubmitIdentityDocumentAsync(Request.UploadIdentityDocumentRequest request)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        
        var userIdGuid = Guid.Parse(userId!);
        
        var frontIdUrl = string.Empty;
        var backIdUrl = string.Empty;
        var selfieUrl = string.Empty;
        if (request.IdCardFrontUrl != null)
        {
            frontIdUrl = await _mediaService.UploadAsync(request.IdCardFrontUrl);
        }

        if (request.IdCardBackUrl != null)
        {
            backIdUrl = await _mediaService.UploadAsync(request.IdCardBackUrl);
        }

        if (selfieUrl != null)
        {
            selfieUrl = await _mediaService.UploadAsync(request.SelfieUrl);
        }
        
        var identityDocument = new IdentityDocument()
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            IdCardFrontUrl = frontIdUrl,
            IdCardBackUrl = backIdUrl,
            SelfieUrl = selfieUrl,
            Status = IdentityStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        _dbContext.IdentityDocuments.Add(identityDocument);
        var check = await _dbContext.SaveChangesAsync();
        

        if (check > 0)
        {
            return "Submit identity document successfully";
        }

        return "Submit identity document failed";
    }

    public async Task<string> ReSubmitIdentityDocumentAsync(Request.ReUploadIdentityDocumentRequest request)
    {
        var identityDocument = await _dbContext.IdentityDocuments.FirstOrDefaultAsync(x => x.Id == request.DocumentId );

        if (identityDocument == null)
        {
            return "Identity document not found";
        }
        
        var frontIdUrl = string.Empty;
        var backIdUrl = string.Empty;
        var selfieUrl = string.Empty;
        if (request.IdCardFrontUrl != null)
        {
            frontIdUrl = await _mediaService.UploadAsync(request.IdCardFrontUrl);
        }

        if (request.IdCardBackUrl != null)
        {
            backIdUrl = await _mediaService.UploadAsync(request.IdCardBackUrl);
        }

        if (selfieUrl != null)
        {
            selfieUrl = await _mediaService.UploadAsync(request.SelfieUrl);
        }
        
        identityDocument.IdCardFrontUrl = frontIdUrl;
        identityDocument.IdCardBackUrl = backIdUrl;
        identityDocument.SelfieUrl = selfieUrl;
        identityDocument.Status = IdentityStatus.Pending;
        identityDocument.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.IdentityDocuments.Update(identityDocument);
        var check = await _dbContext.SaveChangesAsync();

        if (check > 0)
        {
            return "Re-submit identity document successfully";
        }

        return "Re-submit identity document failed";
    }

    public async Task<Response.IdentityDocumentResponse> GetMyDocumentAsync()
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        
        var userIdGuid = Guid.Parse(userId!);
        var identityDocument = await _dbContext.IdentityDocuments.FirstOrDefaultAsync(x => x.UserId == userIdGuid);

        if (identityDocument == null)
        {
            throw new Exception("Identity document not found");
        }

        var query = _dbContext.IdentityDocuments.Include(x => x.User);

        var response = new Response.IdentityDocumentResponse
        {
            Id = identityDocument.Id,
            IdCardFrontUrl = identityDocument.IdCardFrontUrl,
            IdCardBackUrl = identityDocument.IdCardBackUrl,
            SelfieUrl = identityDocument.SelfieUrl,
            Status = identityDocument.Status,
            CreatedAt = identityDocument.CreatedAt,
            Note = identityDocument.Note,
            VerifiedAt = identityDocument.VerifiedAt
        };

        return response;
    }

    public async Task<Base.Response.PageResult<Response.IdentityDocumentAdminResponse>> GetAllAsync(IdentityStatus? status, int pageIndex, int pageSize)
    {
        var query = _dbContext.IdentityDocuments
            .Include(x => x.User)
            .AsQueryable();


        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);


        var pageResult = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);

        var select = pageResult.Select(x => new Response.IdentityDocumentAdminResponse
        {
            Id = x.Id,
            IdCardFrontUrl = x.IdCardFrontUrl,
            IdCardBackUrl = x.IdCardBackUrl,
            SelfieUrl = x.SelfieUrl,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            Note = x.Note,
            VerifiedAt = x.VerifiedAt,
            User = new Response.UserSummary
            {
                Id = x.User.Id,
                FullName = x.User.FullName,
                ProfilePicture = x.User.ProfilePicture,
                Email = x.User.Email
            }
        });

        var total = await query.CountAsync();
        var items = await select.ToListAsync();

        var result = new Base.Response.PageResult<Response.IdentityDocumentAdminResponse>
        {
            Items = items,
            TotalItems = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        return result;

    }

    public async Task<Response.IdentityDocumentAdminResponse> GetByIdAsync(Guid documentId)
    {
        var identityDocument = await _dbContext.IdentityDocuments
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == documentId);

        if (identityDocument == null)
        {
            throw new Exception("Identity document not found");
        }

        var result = new Response.IdentityDocumentAdminResponse()
        {
            Id = identityDocument.Id,
            IdCardFrontUrl = identityDocument.IdCardFrontUrl,
            IdCardBackUrl = identityDocument.IdCardBackUrl,
            SelfieUrl = identityDocument.SelfieUrl,
            Status = identityDocument.Status,
            CreatedAt = identityDocument.CreatedAt,
            Note = identityDocument.Note,
            VerifiedAt = identityDocument.VerifiedAt,
            User = new Response.UserSummary
            {
                Id = identityDocument.User.Id,
                FullName = identityDocument.User.FullName,
                ProfilePicture = identityDocument.User.ProfilePicture,
                Email = identityDocument.User.Email
            }
        };
        return result;

    }

    public async Task<string> ApproveAsync(Guid documentId)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        
        var adminIdGuid = Guid.Parse(userId!);
        var identityDocument = await _dbContext.IdentityDocuments
        .FirstOrDefaultAsync(x => x.Id == documentId);

        if (identityDocument == null)
        {
            throw new Exception("Identity document not found");
        }

        identityDocument.Status = IdentityStatus.Verified;
        identityDocument.VerifiedAt = DateTimeOffset.UtcNow;
        identityDocument.VerifiedBy = adminIdGuid.ToString();
        identityDocument.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.IdentityDocuments.Update(identityDocument);
        var check = await _dbContext.SaveChangesAsync();

        if (check > 0)
        {
            return "Approve identity document successfully";
        }

        return "Approve identity document failed";
    }

    public async Task<string> RejectAsync(Guid documentId, string reason)
    {
        var userId = _httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
        
        var adminIdGuid = Guid.Parse(userId!);
        var identityDocument = await _dbContext.IdentityDocuments
        .FirstOrDefaultAsync(x => x.Id == documentId);

        if (identityDocument == null)
        {
            throw new  Exception("Identity document not found");
        }

        identityDocument.Status = IdentityStatus.Rejected;
        identityDocument.Note = reason;
        identityDocument.VerifiedAt = DateTimeOffset.UtcNow;
        identityDocument.VerifiedBy = adminIdGuid.ToString();
        identityDocument.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.IdentityDocuments.Update(identityDocument);
        var check = await _dbContext.SaveChangesAsync();

        if (check > 0)
        {
            return "Reject identity document successfully";
        }

        return "Reject identity document failed";
    }
}