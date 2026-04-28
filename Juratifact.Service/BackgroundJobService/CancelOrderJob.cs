using Quartz;
using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.BackgroundJobService;

public class CancelOrderJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public CancelOrderJob(AppDbContext dbContext, ILogger<CancelOrderJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Đang chạy CancelOrderJob: Quét các đơn hàng hết hạn thanh toán...");
        
        // Ngưỡng thời gian: 10 phút trước
        var timeoutThreshold = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Tìm các giao dịch OrderPayment đang Pending và quá 10 phút
        var pendingTransactions = await _dbContext.Transactions
            .Include(t => t.Order)
            .Where(t => t.TransactionType == TransactionType.OrderPayment 
                        && t.Status == TransactionStatus.Pending 
                        && t.CreatedAt < timeoutThreshold)
            .ToListAsync();

        if (pendingTransactions.Any())
        {
            foreach (var trans in pendingTransactions)
            {
                trans.Status = TransactionStatus.Failed;
                trans.Description = "Hệ thống tự động hủy do quá 10 phút không thanh toán.";
                
                if (trans.Order != null)
                {
                    trans.Order.Status = OrderStatus.Cancelled; //
                    trans.Order.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Đã hủy thành công {Count} đơn hàng treo.", pendingTransactions.Count);
        }
    }
}