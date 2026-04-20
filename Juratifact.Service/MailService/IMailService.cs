namespace Juratifact.Service.MailService;

public interface IMailService
{
    public Task SendMail(MailContent mailContent);
}

public class MailContent
{
    public required string To { get; set; }         //Dịa chỉ gửi đến
    public required string Subject { get; set; }    // Chủ đề
    public required string Body { get; set; }       // Nội dung
}