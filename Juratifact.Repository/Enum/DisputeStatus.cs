namespace Juratifact.Repository.Enum;

public enum DisputeStatus
{
    Pending, // Mới tạo: Người mua vừa gửi khiếu nại, chờ Admin hoặc Seller tiếp nhận.
    Processing, // Đang xử lý: Admin đã tiếp nhận và đang xem xét bằng chứng.
    WaitingForBuyer, // Chờ người mua: Admin yêu cầu người mua cung cấp thêm video/hình ảnh mở hàng.
    WaitingForSeller, // Chờ người bán: Admin yêu cầu người bán giải trình hoặc cung cấp video đóng gói.
    Resolved, // Đã giải quyết: Vụ việc đã có phán quyết cuối cùng và được đóng lại.
    Cancelled // Đã hủy: Người mua tự rút lại/hủy khiếu nại.
}