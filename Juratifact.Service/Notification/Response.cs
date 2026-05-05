namespace Juratifact.Service.Notification;

public class Response
{
    public class GetNotificationResponse
    {
        public Guid NotificationId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? RedirectUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}