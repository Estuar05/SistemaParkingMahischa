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
    public string? Reference { get; set; }
    public decimal? TenderedAmount { get; set; }
    public decimal? ChangeAmount { get; set; }
}

public static class PaymentMethods
{
    public const string Cash = "Efectivo";
    public const string Sinpe = "SINPE";

    public static readonly string[] All = [Cash, Sinpe];
}

