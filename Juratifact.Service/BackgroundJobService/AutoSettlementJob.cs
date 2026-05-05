using Juratifact.Repository;
using Juratifact.Repository.Enum;
using Juratifact.Service.SettlementService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Juratifact.Service.BackgroundJobService;

[DisallowConcurrentExecution] 
public class AutoSettlementJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ISettlementService _settlementService;
    private readonly ILogger<AutoSettlementJob> _logger;

    public AutoSettlementJob(
        AppDbContext dbContext, 
        ISettlementService settlementService, 
        ILogger<AutoSettlementJob> logger)
    {
        _dbContext = dbContext;
        _settlementService = settlementService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Bắt đầu chạy AutoSettlementJob...");

        try
        {
            var thresholdTime = DateTimeOffset.UtcNow.AddHours(-48);
            
            var orderIdsToSettle = await _dbContext.Orders
                .Where(o => o.Status == OrderStatus.Delivered 
                            // SỬA LẠI THÀNH DeliveredAt (hoặc trường thời gian tương đương trong DB của bạn)
                            && o.DeliveryAt <= thresholdTime 
                            && o.IsDeleted == false)
                .Select(o => o.Id)
                .ToListAsync();

            if (!orderIdsToSettle.Any())
            {
                _logger.LogInformation("Không có đơn hàng nào cần Auto-Settlement lúc này.");
                return; 
            }

            _logger.LogInformation($"Tìm thấy {orderIdsToSettle.Count} đơn hàng cần xử lý tự động.");

            foreach (var orderId in orderIdsToSettle)
            {
                try 
                {
                    bool isSuccess = await _settlementService.ProcessSettlementAsync(orderId);

                    if (isSuccess)
                    {
                        _logger.LogInformation($"[SUCCESS] Đã tự động chốt đơn và chia tiền cho OrderId: {orderId}");
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, $"[FAIL] Lỗi khi xử lý Auto-Settlement cho OrderId: {orderId}");
                }
            }

            _logger.LogInformation("Hoàn tất chạy AutoSettlementJob.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi nghiêm trọng khi thực thi AutoSettlementJob");
        }
    }
}