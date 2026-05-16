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

            var sellerOrderIdsToSettle = await _dbContext.SellerOrders
                .Where(so => so.IsDeleted == false
                             && so.Status == OrderStatus.Delivered
                             && so.DeliveryAt != null
                             && so.Order.IsDeleted == false
                             && so.Order.PaymentStatus == PaymentStatus.Paid)
                .Where(so => so.DeliveryAt <= thresholdTime)
                .Select(so => so.Id)
                .ToListAsync();

            if (sellerOrderIdsToSettle.Count == 0)
            {
                _logger.LogInformation("Khong co seller order nao can Auto-Settlement luc nay.");
                return;
            }

            _logger.LogInformation("Tim thay {Count} seller order can xu ly tu dong.", sellerOrderIdsToSettle.Count);

            foreach (var sellerOrderId in sellerOrderIdsToSettle)
            {
                try
                {
                    var isSuccess = await _settlementService.ProcessSettlementAsync(sellerOrderId);
                    if (isSuccess)
                    {
                        _logger.LogInformation("[SUCCESS] Da tu dong settlement cho SellerOrderId: {SellerOrderId}", sellerOrderId);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "[FAIL] Loi khi xu ly Auto-Settlement cho SellerOrderId: {SellerOrderId}", sellerOrderId);
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
