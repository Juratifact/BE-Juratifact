using Juratifact.Service.DiscordService;
using Juratifact.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.API.Middlewares;

public class GlobalExceptionHandlerMiddleware: IMiddleware
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IDiscordService _discordService;

    public GlobalExceptionHandlerMiddleware(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IDiscordService discordService)
    {
        _environment = environment;
        _logger = logger;
        _discordService = discordService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request {Path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the global exception middleware will not write an error response");
                throw;
            }

            var statusCode = MapStatusCode(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            
            // chỉ gửi Discord khi là lỗi 500
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                try 
                {
                    await _discordService.SendExceptionAlertAsync(context, ex, statusCode);
                }
                catch (Exception discordEx)
                {
                    // Chỉ log ra file/console hệ thống, tuyệt đối không làm đứt gãy luồng trả Response về cho Client
                    _logger.LogError(discordEx, "Failed to send exception alert to Discord");
                }
            }

            var response = ApiResponseFactory.ErrorResponse(
                ResolveClientMessage(ex, statusCode),
                _environment.IsDevelopment() ? new { detail = ex.Message } : null,
                context.TraceIdentifier);

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static int MapStatusCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            BadHttpRequestException => StatusCodes.Status413PayloadTooLarge,
            InvalidDataException => StatusCodes.Status413PayloadTooLarge,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string ResolveClientMessage(Exception ex, int statusCode)
    {
        if (statusCode >= 500)
            return "An unexpected error occurred";
        if (statusCode == StatusCodes.Status413PayloadTooLarge)
            return "Video vượt quá dung lượng tối đa 100MB.";
        if (ex is DbUpdateException)
            return "The data could not be saved. Please check your input and try again.";
        return ex.Message;
    }
}
