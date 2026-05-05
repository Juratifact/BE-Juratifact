namespace Juratifact.Service.Notification;

public interface INotificationService
{
        public Task SendNotification(Request.SendNotificationRequest request);
        public Task MarkAsRead(Guid notificationId);
        public Task<Base.Response.PageResult<Response.GetNotificationResponse>> GetNotificationsByUserId(Guid userId, int pageIndex, int pageSize);
}