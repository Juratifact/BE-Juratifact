using Juratifact.Repository;
using Juratifact.Service.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace Juratifact.Service.Notification;

public class NotificationService: INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;
    public NotificationService(AppDbContext dbContext,  IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }
    public async Task SendNotification(Request.SendNotificationRequest request)
    {
        var notification = new Repository.Entity.Notification()
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title,
            Content = request.Content,
            Type = request.Type,
            RedirectUrl = request.RedirectUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Đẩy real-time tới client
        await _hubContext.Clients.User(request.UserId.ToString())
            .SendAsync("ReceiveNotification", new Response.GetNotificationResponse() 
            { 
                NotificationId = notification.Id,
                Title = notification.Title,
                Content = notification.Content,
                RedirectUrl = notification.RedirectUrl
            });
    }

    public async Task MarkAsRead(Guid notificationId)
    {
        var notification = await _dbContext.Notifications.FindAsync(notificationId);
        
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Base.Response.PageResult<Response.GetNotificationResponse>> GetNotificationsByUserId(Guid userId, int pageIndex, int pageSize)
    {
        var query = _dbContext.Notifications.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt);
        
        var pageResult = query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
        
        var select = pageResult.Select(x => new Response.GetNotificationResponse()
        {
            NotificationId = x.Id,
            Title = x.Title,
            Content = x.Content,
            RedirectUrl = x.RedirectUrl,
            IsRead = x.IsRead,
            CreatedAt = x.CreatedAt
        });
        
        var total = select.Count();
        var item = await select.ToListAsync();

        var result = new Base.Response.PageResult<Response.GetNotificationResponse>()
        {
            TotalItems = total,
            Items = item,
            PageIndex = pageIndex,
            PageSize = pageSize,
        };
        
        return result;

    }
}