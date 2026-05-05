using Juratifact.Service.Models;
using Juratifact.Service.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController: ControllerBase
{
    private readonly INotificationService _notificationService;
    
    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPut("MarkAsRead")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        await _notificationService.MarkAsRead(notificationId);
        return Ok(ApiResponseFactory.SuccessResponse(null, "Notification marked as read", HttpContext.TraceIdentifier));
    }
    
    [HttpGet("GetNotifications")]
    public async Task<IActionResult> GetNotifications(Guid userId, int pageIndex = 1, int pageSize = 10)
    {
        var notifications = await _notificationService.GetNotificationsByUserId(userId, pageIndex, pageSize);
        return Ok(ApiResponseFactory.SuccessResponse(notifications, "Notifications retrieved", HttpContext.TraceIdentifier));
    }
}