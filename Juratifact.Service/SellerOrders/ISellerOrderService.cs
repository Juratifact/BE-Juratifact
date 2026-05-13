using Juratifact.Repository.Enum;
using Juratifact.Service.Base;

namespace Juratifact.Service.SellerOrders;

public interface ISellerOrderService
{
    Task<Response.PageResult<SellerOrderResponse>> GetMySellerOrders(
        OrderStatus? status,
        int pageSize,
        int pageIndex);

    Task<List<SellerOrderResponse>> GetSellerOrdersByParentOrderId(Guid orderId);

    Task<SellerOrderResponse> GetSellerOrderById(Guid sellerOrderId);

    Task<List<SellerOrderTransactionResponse>> GetSellerOrderTransactions(Guid sellerOrderId);
}
