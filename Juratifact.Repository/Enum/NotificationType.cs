namespace Juratifact.Repository.Enum;

public enum NotificationType
{
    System,             // Thông báo chung từ hệ thống

    // Đơn hàng & Sản phẩm
    OrderUpdate,        // Cập nhật trạng thái đơn hàng (đã đặt, đã ship, đã nhận)
    ProductStatus,      // Thông báo về trạng thái sản phẩm (hết hàng, đã bán, bị ẩn)

    // Tài chính (Ví tiền)
    WalletTransaction,  // Tiền về ví, trừ tiền thành công
    Refund,             // Hoàn tiền

    // Tranh chấp & Báo cáo
    ReportAlert,        // Báo cáo vi phạm
    DisputeUpdate,      // Cập nhật trạng thái tranh chấp (đang xử lý, đã giải quyết)

    // Tương tác xã hội
    CommentReply,       // Có người trả lời bình luận
    Promotion           // Khuyến mãi/Ưu đãi
}