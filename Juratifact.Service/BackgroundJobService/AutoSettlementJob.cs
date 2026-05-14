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
        _logger.LogInformation("Bat dau chay AutoSettlementJob...");

        try
        {
            var thresholdTime = DateTimeOffset.UtcNow.AddHours(-48);

            // New order flow: Order.DeliveryAt is not set; delivery time lives on SellerOrder.DeliveryAt.
            // Only auto-settle after 48h from the LAST delivery of the order (max DeliveryAt).
            var orderIdsToSettle = await _dbContext.SellerOrders
                .Where(so => so.IsDeleted == false
                             && so.Status == OrderStatus.Delivered
                             && so.DeliveryAt != null
                             && so.Order.IsDeleted == false
                             && so.Order.Status == OrderStatus.Delivered
                             && so.Order.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(so => so.OrderId)
                .Where(g => g.Max(so => so.DeliveryAt) <= thresholdTime)
                .Select(g => g.Key)
                .ToListAsync();

            if (orderIdsToSettle.Count == 0)
            {
                _logger.LogInformation("Khong co don hang nao can Auto-Settlement luc nay.");
                return;
            }

            _logger.LogInformation("Tim thay {Count} don hang can xu ly tu dong.", orderIdsToSettle.Count);

            foreach (var orderId in orderIdsToSettle)
            {
                try
                {
                    var isSuccess = await _settlementService.ProcessSettlementAsync(orderId);
                    if (isSuccess)
                    {
                        _logger.LogInformation("[SUCCESS] Da tu dong settlement cho OrderId: {OrderId}", orderId);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[FAIL] Loi khi xu ly Auto-Settlement cho OrderId: {OrderId}", orderId);
                }
            }

            _logger.LogInformation("Hoan tat chay AutoSettlementJob.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi nghiem trong khi thuc thi AutoSettlementJob");
        }
    }
}
