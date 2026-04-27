namespace Juratifact.Service.Sepay;

public interface ISepayService
{
    public Task<bool> ProcessSePayWebhook(Request.SepayWebhookDto data);
    public Task<string> GenerateQrCode(decimal amount, string referenceCode);
}