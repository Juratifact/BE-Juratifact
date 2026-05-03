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
    // 1. Chống xử lý trùng (Idempotency)
    if (await _dbContext.Transactions.AnyAsync(t => t.SepayId == data.SepayId.ToString()))
    {
        _logger.LogInformation("Giao dịch {Id} đã được xử lý.", data.SepayId);
        return true; 
    }

    // 2. Tìm Transaction kèm các liên kết cần thiết
    var transaction = await _dbContext.Transactions
        .Include(t => t.UserPromotionSubscription)
            .ThenInclude(s => s.PromotionPackage)
        .Include(t => t.Order) // Giả định bạn có navigation property Order
        .FirstOrDefaultAsync(t => 
            !string.IsNullOrEmpty(t.ReferenceCode) &&
            data.Content.ToUpper().Contains(t.ReferenceCode.ToUpper()) &&
            t.Status == TransactionStatus.Pending);

    if (transaction == null) return false;

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
        transaction.SepayId = data.SepayId.ToString();
        transaction.ExternalTransactionId = data.ReferenceCode;
        transaction.Description = $"Thanh toán qua {data.Gateway} lúc {data.TransactionDate}";
        transaction.UpdatedAt = DateTime.Now;

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
                // 2. Cập nhật trạng thái đơn hàng
                order.Status = OrderStatus.Paid; // Giả sử bạn có Enum này
                order.PaymentStatus = PaymentStatus.Paid; // Đảm bảo PaymentStatus cũng cập nhật
                order.UpdatedAt = DateTimeOffset.UtcNow; // Sử dụng UtcNow đồng bộ với project

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

                // 4. Lưu tất cả thay đổi
                await _dbContext.SaveChangesAsync();

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
