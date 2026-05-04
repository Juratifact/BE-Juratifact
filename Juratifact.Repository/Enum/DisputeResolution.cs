namespace Juratifact.Repository.Enum;

public enum DisputeResolution
{
    None = 0, // Chưa có quyết định (Giá trị mặc định khi khiếu nại đang ở trạng thái Pending/Processing).
    FullRefund = 1, // Hoàn tiền toàn bộ: Admin xử thắng cho Buyer, tiền được trả lại 100%.
    PartialRefund = 2, // Hoàn tiền một phần: Hàng bị lỗi nhẹ, Buyer giữ hàng và nhận lại một phần tiền.
    ReturnAndRefund = 3, // Trả hàng & Hoàn tiền: Buyer phải gửi trả hàng về cho Seller thì mới được hoàn tiền.
    Rejected = 4 // Từ chối khiếu nại: Bằng chứng của Buyer không hợp lệ, xử thắng cho Seller, tiền vẫn được thanh toán cho Seller.
}