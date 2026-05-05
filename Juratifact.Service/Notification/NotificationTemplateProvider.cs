using Juratifact.Repository.Enum;

namespace Juratifact.Service.Notification;

public static class NotificationTemplateProvider
{
    public static (string Title, string Content) GetTemplate(NotificationType type, string? data)
    {
        return type switch
        {
            NotificationType.System => ("Thông báo hệ thống", data ?? "Bạn có thông báo mới."),
            
            NotificationType.OrderPlaced => 
                ("Đặt hàng thành công", $"Đơn hàng {data} của bạn đã được xác nhận."),
                
            NotificationType.OrderShipped => 
                ("Đơn hàng đang giao", $"Đơn hàng {data} đang trên đường đến tay bạn."),
                
            NotificationType.OrderDelivered => 
                ("Giao hàng thành công", $"Đơn hàng {data} đã được giao thành công. Cảm ơn bạn!"),
                
            NotificationType.OrderCancelled => 
                ("Đơn hàng đã hủy", $"Đơn hàng {data} đã được hủy theo yêu cầu."),
                
            NotificationType.OrderReturned => 
                ("Hoàn trả đơn hàng", $"Yêu cầu hoàn trả cho đơn hàng {data} đang được xử lý."),
            
            NotificationType.ProductStatus => ("Thông báo sản phẩm", $"Sản phẩm {data} đã thay đổi trạng thái."),
            
            NotificationType.WalletTransaction => ("Biến động số dư", $"Ví của bạn vừa nhận được: {data}"),
            NotificationType.Refund => ("Hoàn tiền", $"Đơn hàng {data} đã được hoàn tiền về ví."),
            
            NotificationType.ReportAlert => ("Cảnh báo vi phạm", $"Hệ thống nhận được báo cáo: {data}"),
            NotificationType.DisputeUpdate => ("Cập nhật tranh chấp", $"Tranh chấp đơn hàng {data} đã có diễn biến mới."),
            
            NotificationType.CommentReply => ("Phản hồi bình luận", $"Có người đã trả lời bình luận: {data}"),
            NotificationType.Promotion => ("Ưu đãi", $"Khám phá ngay: {data}"),
            
            _ => ("Thông báo", "Bạn có thông báo mới.")
        };
    }
}