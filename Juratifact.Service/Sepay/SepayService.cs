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

   
    if (!string.IsNullOrWhiteSpace(data.TransferType) &&
        !string.Equals(data.TransferType, "in", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogInformation("Bỏ qua webhook transferType={TransferType}, id={Id}", data.TransferType, sepayEventId);
        return true;
    }

    
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

      
        transaction = existingTransaction;
    }

    
    var content = data.Content ?? string.Empty;
    var reference = data.ReferenceCode ?? string.Empty;

    if (transaction == null)
    {
        transaction = await _dbContext.Transactions
            .Include(t => t.UserPromotionSubscription)
                .ThenInclude(s => s.PromotionPackage)
            .Include(t => t.Order)
            .Where(t =>
                !string.IsNullOrEmpty(t.ReferenceCode) &&
                (
                    
                    (!string.IsNullOrEmpty(reference) && EF.Functions.ILike(t.ReferenceCode, reference)) ||
                   
                    (!string.IsNullOrEmpty(content) && EF.Functions.ILike(content, "%" + t.ReferenceCode + "%"))
                ) &&
               
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
       
        transaction.Status = TransactionStatus.Success;
        if (!string.IsNullOrWhiteSpace(sepayEventId))
        {
            transaction.SepayId ??= sepayEventId;
        }
        transaction.ExternalTransactionId = data.ReferenceCode;
        transaction.Description = $"Thanh toán qua {data.Gateway} lúc {data.TransactionDate}";
        transaction.UpdatedAt = DateTimeOffset.UtcNow;

       
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

        if (sub == null && transaction.UserPromotionSubscriptionId.HasValue)
        {
            sub = await _dbContext.UserPromotionSubscriptions
                .Include(s => s.PromotionPackage)
                .FirstOrDefaultAsync(s => s.Id == transaction.UserPromotionSubscriptionId.Value);

            transaction.UserPromotionSubscription = sub;
        }

        if (sub != null && sub.PromotionPackage == null)
        {
            await _dbContext.Entry(sub).Reference(s => s.PromotionPackage).LoadAsync();
        }

        if (sub == null)
        {
            _logger.LogWarning(
                "Không tìm thấy subscription cho transaction {TransactionId} (UserPromotionSubscriptionId={SubscriptionId})",
                transaction.Id, transaction.UserPromotionSubscriptionId);
            return;
        }

        if (sub.PromotionPackage == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} không có PromotionPackage", sub.Id);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var usageLimitDays = sub.PromotionPackage.UsageLimitDays.GetValueOrDefault(30);
        if (usageLimitDays <= 0)
        {
            usageLimitDays = 30;
        }

        sub.PaymentStatus = PaymentStatus.Paid;
        sub.StartTime = now;
        sub.EndTime = now.AddDays(usageLimitDays);
        sub.TotalSlot = sub.PromotionPackage.MaxProductCount;
        sub.UsedSlot = 0;
        sub.TransactionId ??= transaction.Id;
        sub.UpdatedAt = now;

        _logger.LogInformation("Đã kích hoạt gói {Name} cho User {User}",
            sub.PromotionPackage.PackageName, sub.UserId);
    }
    
    private async Task HandleOrderPayment(Transaction transaction)
    {
        
        if (transaction.OrderId != null)
        {
            
            var order = await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.SellerOrders)
                .FirstOrDefaultAsync(o => o.Id == transaction.OrderId);

            if (order != null)
            {
               
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

                
                order.Status = OrderStatus.Paid;
                order.PaymentStatus = PaymentStatus.Paid;
                order.UpdatedAt = DateTimeOffset.UtcNow; 

                foreach (var sellerOrder in order.SellerOrders.Where(so => so.Status == OrderStatus.PendingPayment))
                {
                    sellerOrder.Status = OrderStatus.Paid;
                    sellerOrder.UpdatedAt = DateTimeOffset.UtcNow;
                }

                
                var productIds = order.OrderDetails.Select(od => od.ProductId).ToList();

                
                var products = await _dbContext.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                
                foreach (var product in products)
                {
                    product.Status = ProductStatus.Sold; 
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
