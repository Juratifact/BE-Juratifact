using Quartz;
using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Juratifact.Service.BackgroundJobService;

public class SubscriptionExpiryJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public SubscriptionExpiryJob(AppDbContext dbContext, ILogger<SubscriptionExpiryJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Đang chạy SubscriptionExpiryJob: Kiểm tra thời hạn gói dịch vụ...");

        var now = DateTimeOffset.UtcNow;
        // Tìm các gói đã quá EndTime nhưng vẫn đang là Paid
        var expiredSubs = await _dbContext.UserPromotionSubscriptions
            .Where(s => s.EndTime < now && s.PaymentStatus == PaymentStatus.Paid)
            .ToListAsync();

        foreach (var sub in expiredSubs)
        {
            // Tùy chọn: Bạn có thể cập nhật trạng thái sang Expired nếu có Enum này
            // sub.PaymentStatus = PaymentStatus.Expired;
            
            _logger.LogWarning("Gói dịch vụ {SubId} của User {UserId} đã hết hạn vào lúc {EndTime}", 
                sub.Id, sub.UserId, sub.EndTime);
        }

        if (expiredSubs.Any())
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Đã xử lý {Count} gói dịch vụ hết hạn.", expiredSubs.Count);
        }
    }
}