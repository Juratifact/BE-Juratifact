namespace Juratifact.Service.Transactionss;

public interface ITransactionServices
{
    public Task<Base.Response.PageResult<Response.TransactionResponse>> 
        GetAllTransactions(Request.TransactionRequest request, int pageIndex, int pageSize);
    
    public Task<Base.Response.PageResult<Response.TransactionResponse>> 
        GetTransactionsBySellerId(Request.TransactionRequest request,Guid sellerId, int pageIndex, int pageSize);
}