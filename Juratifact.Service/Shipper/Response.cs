namespace Juratifact.Service.Shipper;

public class Response
{
    public class ShipperResponse
    {
        public Guid OrderId { get; set; }
        public string? AddressSeller { get; set; } 
        public string? AddressBuyer { get; set; }
        public decimal TotalPrice { get; set; }

    }
}