namespace SistemaParkingMahischa.Models;

public sealed class Payment
{
    public long PaymentId { get; set; }
    public long SessionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Efectivo";
}

