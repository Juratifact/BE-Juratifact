using Microsoft.AspNetCore.Http;

namespace Juratifact.Service.DiscordService;

public interface IDiscordService
{
    Task SendExceptionAlertAsync(
        HttpContext context,
        Exception exception,
        int statusCode,
        CancellationToken cancellationToken = default);
}