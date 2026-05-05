using Juratifact.Repository.Enum;

namespace Juratifact.Service.Notification;

public class Request
{
    public class SendNotificationRequest
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public string? RedirectUrl { get; set; }
    }
}