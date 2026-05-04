namespace Juratifact.Repository.Enum;

public enum DisputeResolution
{
    None, // Chưa có quyết định (Giá trị mặc định khi khiếu nại đang ở trạng thái Pending/Processing).
    FullRefund, // Hoàn tiền toàn bộ: Admin xử thắng cho Buyer, tiền được trả lại 100%.
    PartialRefund, // Hoàn tiền một phần: Hàng bị lỗi nhẹ, Buyer giữ hàng và nhận lại một phần tiền.
    ReturnAndRefund, // Trả hàng & Hoàn tiền: Buyer phải gửi trả hàng về cho Seller thì mới được hoàn tiền.
    Rejected // Từ chối khiếu nại: Bằng chứng của Buyer không hợp lệ, xử thắng cho Seller, tiền vẫn được thanh toán cho Seller.
}