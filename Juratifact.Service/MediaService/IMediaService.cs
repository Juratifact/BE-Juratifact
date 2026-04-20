using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.MediaService;

public interface IMediaService
{
    public Task<string> UploadAsync(IFormFile file);
}