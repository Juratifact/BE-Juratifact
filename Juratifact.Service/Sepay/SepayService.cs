using Juratifact.Repository;
using Juratifact.Repository.Entity;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.Sepay;

public class SepayService: ISepayService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;
    public SepayService(IConfiguration configuration,  AppDbContext dbContext, ILogger<SepayService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

   public async Task<bool> ProcessSePayWebhook(Request.SepayWebhookDto data)
{
    var sepayEventId = data.Id > 0 ? data.Id.ToString() : null;

    // Chỉ xử lý giao dịch tiền vào
    if (!string.IsNullOrWhiteSpace(data.TransferType) &&
        !string.Equals(data.TransferType, "in", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogInformation("Bỏ qua webhook transferType={TransferType}, id={Id}", data.TransferType, sepayEventId);
        return true;
    }

    // 1. Chống xử lý trùng (Idempotency)
    // Nếu SepayId đã tồn tại nhưng Order chưa được set `PaymentStatus = Paid` thì cho phép xử lý lại.
    Transaction? existingTransaction = null;
    if (!string.IsNullOrWhiteSpace(sepayEventId))
    {
        existingTransaction = await _dbContext.Transactions
            .Include(t => t.Order)
            .Include(t => t.UserPromotionSubscription)
                .ThenInclude(s => s.PromotionPackage)
            .FirstOrDefaultAsync(t => t.SepayId == sepayEventId);
    }

    Transaction? transaction = null;

    if (existingTransaction != null && existingTransaction.Status == TransactionStatus.Success)
    {
        if (existingTransaction.TransactionType == TransactionType.OrderPayment &&
            existingTransaction.Order != null &&
            (existingTransaction.Order.PaymentStatus == PaymentStatus.Paid ||
             existingTransaction.Order.PaymentStatus == PaymentStatus.Settled))
        {
            _logger.LogInformation("Giao dịch {Id} đã được xử lý (order payment already {PaymentStatus}).",
                sepayEventId, existingTransaction.Order.PaymentStatus);
            return true;
        }

        _logger.LogInformation(
            "Giao dịch {Id} đã tồn tại nhưng order payment chưa ở trạng thái Paid (actual: {PaymentStatus}). Tiếp tục xử lý lại.",
            sepayEventId, existingTransaction.Order?.PaymentStatus);

        // Re-use bản ghi đã Success để self-heal trạng thái order/subscription nếu lần trước cập nhật dở dang.
        transaction = existingTransaction;
    }

    // 2. Tìm Transaction kèm các liên kết cần thiết (nếu chưa có bản ghi để re-use)
    var content = data.Content ?? string.Empty;
    var reference = data.ReferenceCode ?? string.Empty;

    if (transaction == null)
    {
        transaction = await _dbContext.Transactions
            .Include(t => t.UserPromotionSubscription)
                .ThenInclude(s => s.PromotionPackage)
            .Include(t => t.Order) // Giả định bạn có navigation property Order
            .Where(t =>
                !string.IsNullOrEmpty(t.ReferenceCode) &&
                (
                    // 1) Trường hợp webhook trả về reference khớp với `des` (referenceCode trong QR)
                    (!string.IsNullOrEmpty(reference) && EF.Functions.ILike(t.ReferenceCode, reference)) ||
                    // 2) Trường hợp webhook trả về `content` chứa reference (không phân biệt hoa thường)
                    (!string.IsNullOrEmpty(content) && EF.Functions.ILike(content, "%" + t.ReferenceCode + "%"))
                ) &&
                // Cho phép xử lý cả Pending và Failed để bắt trường hợp timeout cancel nhưng thực tế vẫn thanh toán thành công.
                (t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.Failed))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    if (transaction == null)
    {
        var contentPreview = data.Content?.Length > 200
            ? data.Content.Substring(0, 200) + "..."
            : data.Content;

        _logger.LogWarning(
            "Webhook SePay không match được transaction. SepayId={SepayId}, TransferAmount={TransferAmount}, Gateway={Gateway}, ReferenceCode={ReferenceCode}, ContentPreview={ContentPreview}",
            sepayEventId, data.TransferAmount, data.Gateway, data.ReferenceCode, contentPreview);
        return false;
    }

    // 3. Kiểm tra số tiền
    if (data.TransferAmount < transaction.Amount)
    {
        transaction.Status = TransactionStatus.Failed;
        transaction.Description = $"Thanh toán thiếu. Thực nhận: {data.TransferAmount}";
        await _dbContext.SaveChangesAsync();
        return true;
    }

    using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();
    try
    {
        // Cập nhật thông tin chung cho Transaction
        transaction.Status = TransactionStatus.Success;
        if (!string.IsNullOrWhiteSpace(sepayEventId))
        {
            transaction.SepayId ??= sepayEventId;
        }
        transaction.ExternalTransactionId = data.ReferenceCode;
        transaction.Description = $"Thanh toán qua {data.Gateway} lúc {data.TransactionDate}";
        transaction.UpdatedAt = DateTimeOffset.UtcNow;

        // 4. Điều hướng xử lý theo TransactionType
        switch (transaction.TransactionType)
        {
            case TransactionType.ServiceFee:
                await HandlePromotionActivation(transaction);
                break;

            case TransactionType.OrderPayment:
                await HandleOrderPayment(transaction);
                break;

            default:
                _logger.LogWarning("Loại giao dịch {Type} chưa có logic xử lý.", transaction.TransactionType);
                break;
        }

        await _dbContext.SaveChangesAsync();
        await dbTransaction.CommitAsync();
        return true;
    }
    catch (Exception ex)
    {
        await dbTransaction.RollbackAsync();
        _logger.LogError(ex, "Lỗi khi xử lý Webhook cho Transaction {Id}", transaction.Id);
        return false;
    }

}

    public Task<string> GenerateQrCode(decimal amount, string referenceCode)
    {
        {
            var sepayConfig = _configuration.GetSection("SePay");
            string bin = sepayConfig["BankBin"];
            string acc = sepayConfig["AccountNumber"];
            string template = sepayConfig["QrTemplate"];

            var qrLink = $"https://qr.sepay.vn/img?bank={bin}&acc={acc}&template={template}&amount={amount}&des={referenceCode}";
    
            return Task.FromResult(qrLink);
        }
    }
    
    private async Task HandlePromotionActivation(Transaction transaction)
    {
        var sub = transaction.UserPromotionSubscription;
        if (sub != null && sub.PromotionPackage != null)
        {
            sub.PaymentStatus = PaymentStatus.Paid;
            sub.StartTime = DateTime.Now; // StartTime là DateTime không nullable
        
            // Tính EndTime từ UsageLimitDays
            sub.EndTime = sub.StartTime.AddDays((double)sub.PromotionPackage.UsageLimitDays);
        
            // Cấp Slots dựa trên MaxProductCount của gói
            sub.TotalSlot = sub.PromotionPackage.MaxProductCount;
            sub.UsedSlot = 0;

            _logger.LogInformation("Đã kích hoạt gói {Name} cho User {User}", 
                sub.PromotionPackage.PackageName, sub.UserId);
        }
    }
    
    private async Task HandleOrderPayment(Transaction transaction)
    {
        // 1. Kiểm tra nếu Transaction có gắn với OrderId
        if (transaction.OrderId != null)
        {
            // Sử dụng Include để lấy các OrderDetails (cần thiết để biết mua sản phẩm nào)
            var order = await _dbContext.Orders
                .Include(o => o.OrderDetails) 
                .FirstOrDefaultAsync(o => o.Id == transaction.OrderId);

            if (order != null)
            {
                // Tránh ghi đè trạng thái thanh toán đã đi xa (Settled/Refunded/Completed)
                if (order.Status == OrderStatus.Completed)
                {
                    _logger.LogInformation("Order {OrderId} đã Completed, bỏ qua callback cập nhật payment.", order.Id);
                    return;
                }

                if (order.PaymentStatus == PaymentStatus.Settled)
                {
                    _logger.LogInformation("Order {OrderId} đã Settled, bỏ qua chuyển ngược về Paid.", order.Id);
                    return;
                }

                if (order.PaymentStatus == PaymentStatus.Refunded)
                {
                    _logger.LogWarning("Order {OrderId} đã Refunded, bỏ qua callback thanh toán thành công.", order.Id);
                    return;
                }

                // 2. Cập nhật trạng thái đơn hàng
                order.Status = OrderStatus.Paid;
                order.PaymentStatus = PaymentStatus.Paid;
                order.UpdatedAt = DateTimeOffset.UtcNow; 

                // 3. Cập nhật trạng thái sản phẩm thành Sold
                // Lấy danh sách ProductId từ OrderDetails
                var productIds = order.OrderDetails.Select(od => od.ProductId).ToList();

                // Truy vấn các sản phẩm này từ database
                var products = await _dbContext.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                // Chuyển trạng thái từng sản phẩm
                foreach (var product in products)
                {
                    product.Status = ProductStatus.Sold; // Cập nhật sang trạng thái Sold
                }

                _logger.LogInformation("Đơn hàng {OrderId} đã thanh toán. Đã cập nhật trạng thái {Count} sản phẩm thành Sold.", 
                    order.Id, products.Count);
            }
            else
            {
                _logger.LogWarning("Giao dịch OrderPayment {TransId} gắn với OrderId {OrderId} không tồn tại.", 
                    transaction.Id, transaction.OrderId);
            }
        }
    }
}
