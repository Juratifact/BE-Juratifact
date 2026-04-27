using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Juratifact.Repository.Enum;
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
        // 1. Kiểm tra giao dịch đã tồn tại (idempotency)
        if (await _dbContext.Transactions.AnyAsync(t => t.SepayId == data.Id.ToString()))
        {
            _logger.LogInformation("Webhook đã xử lý cho SepayId: {SepayId}", data.Id);
            return true;
        }

        // 2. Tìm transaction khớp với ReferenceCode trong Content và trạng thái chờ xử lý
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t =>
                !string.IsNullOrEmpty(t.ReferenceCode) &&
                !string.IsNullOrEmpty(data.Content) &&
                data.Content.ToUpper().Contains(t.ReferenceCode.ToUpper()) &&
                t.Status == Juratifact.Repository.Enum.TransactionStatus.Pending);

        if (transaction == null)
        {
            _logger.LogWarning("Không tìm thấy transaction phù hợp cho nội dung: {Content}", data.Content);
            return false;
        }

        // 3. Kiểm tra số tiền thực nhận
        if (data.TransferAmount < transaction.Amount)
        {
            // Không có trạng thái 'Insufficient' trong enum → đánh dấu là Failed và ghi mô tả
            transaction.Status = TransactionStatus.Failed; // Thanh toán thiếu được coi là thất bại
            transaction.Description = $"Thanh toán thiếu. Thực nhận: {data.TransferAmount}";
            transaction.UpdatedAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();
            _logger.LogWarning("Thanh toán thiếu cho transaction {Id}. Nhận: {Amount}, Cần: {Need}", transaction.Id, data.TransferAmount, transaction.Amount);
            return true;
        }

        // 4. Cập nhật trạng thái thành công
        transaction.Status = TransactionStatus.Success; // Thành công
        transaction.SepayId = data.Id.ToString();
        transaction.ExternalTransactionId = data.ReferenceCode;
        transaction.UpdatedAt = DateTime.Now;

        // 5. Nếu là thanh toán cho Gói VIP (Subscription)
        // Transaction.UserPromotionSubscriptionId is non-nullable Guid in entity; check != Guid.Empty
        if (transaction.UserPromotionSubscriptionId != Guid.Empty)
        {
            var sub = await _dbContext.UserPromotionSubscriptions
                .FirstOrDefaultAsync(s => s.Id == transaction.UserPromotionSubscriptionId);
            if (sub != null)
            {
                sub.PaymentStatus = PaymentStatus.Paid;
                sub.StartTime = DateTime.Now;
                // TODO: sub.EndTime = sub.StartTime + package duration (nếu có)
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Xử lý thành công webhook cho transaction {Id}", transaction.Id);
        return true;
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
}