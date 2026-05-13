using Juratifact.Repository;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Service.Transactionss;

public class TransactionServices: ITransactionServices
{
    private readonly AppDbContext _dbContext;
    public TransactionServices(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Base.Response.PageResult<Response.TransactionResponse>> GetAllTransactions(Request.TransactionRequest request, int pageIndex, int pageSize)
    {
        var query = _dbContext.Transactions.Where(x => true);

        if (request.TransactionType.HasValue)
        {
            query = query.Where(x =>
                x.TransactionType == request.TransactionType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == request.Status.Value);
        }
        query = query.OrderByDescending(x => x.CreatedAt);
        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        var selected = query.Select(x => new Response.TransactionResponse()
        {
            Id = x.Id,
            SepayId = x.SepayId,
            Amount = x.Amount,
            ExternalTransactionId = x.ExternalTransactionId,
            Description = x.Description,
            ReferenceCode = x.ReferenceCode,
            FeeAmount = x.FeeAmount,
            TransactionType = x.TransactionType,
            Status =  x.Status,
            CreatedAt = x.CreatedAt,
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.TransactionResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }

    public async Task<Base.Response.PageResult<Response.TransactionResponse>> GetTransactionsBySellerId(Request.TransactionRequest request, Guid sellerId, int pageIndex, int pageSize)
    {
        var query = _dbContext.Transactions .Include(t => t.Order)
            .ThenInclude(o => o.OrderDetails) 
            .ThenInclude(od => od.Product) 
            .Where(t => t.Order != null && t.Order.OrderDetails
                .Any(z => z.Product.SellerId == sellerId));
        if (request.TransactionType.HasValue)
        {
            query = query.Where(x =>
                x.TransactionType == request.TransactionType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == request.Status.Value);
        }
        query = query.OrderByDescending(x => x.CreatedAt);
        query = query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
        var selected = query.Select(x => new Response.TransactionResponse()
        {
            Id = x.Id,
            SepayId = x.SepayId,
            Amount = x.Amount,
            ExternalTransactionId = x.ExternalTransactionId,
            Description = x.Description,
            ReferenceCode = x.ReferenceCode,
            FeeAmount = x.FeeAmount,
            TransactionType = x.TransactionType,
            Status =  x.Status,
            CreatedAt = x.CreatedAt,
        });
        var listResult = await selected.ToListAsync();
        var totalItems = listResult.Count;

        var result = new Base.Response.PageResult<Response.TransactionResponse>()
        {
            Items = listResult,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
        return result;
    }
}