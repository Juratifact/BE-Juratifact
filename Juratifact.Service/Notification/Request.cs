using Juratifact.Repository.Enum;

namespace Juratifact.Service.Notification;

public class Request
{
    public class SendNotificationRequest
    {
        public Guid UserId { get; set; }
        public string Data { get; set; }
        public NotificationType Type { get; set; }
    }
}