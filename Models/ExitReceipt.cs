namespace SistemaParkingMahischa.Models;

/// <summary>
/// Datos del comprobante de pago que se imprime al registrar la salida y cobrar.
/// </summary>
public sealed class ExitReceipt
{
    public string Plate { get; init; } = string.Empty;
    public DateTime EntryAt { get; init; }
    public DateTime ExitAt { get; init; }
    public string RateName { get; init; } = string.Empty;
    public decimal BaseAmount { get; init; }
    public decimal ExtraAmount { get; init; }
    public decimal Total { get; init; }
    public string PaymentMethod { get; init; } = PaymentMethods.Cash;
    public decimal? TenderedAmount { get; init; }
    public decimal? ChangeAmount { get; init; }
    public string? Reference { get; init; }
    public string CashierName { get; init; } = string.Empty;

    public static ExitReceipt FromClosedSession(ParkingSession closed, string paymentMethod, decimal? tendered, string? reference, string cashierName)
    {
        var total = closed.ChargedAmount ?? 0m;
        var extra = closed.ExtraAmount ?? 0m;
        return new ExitReceipt
        {
            Plate = closed.Plate,
            EntryAt = closed.EntryAt,
            ExitAt = closed.ExitAt ?? DateTime.Now,
            RateName = closed.HasCustomRate ? "Personalizada" : closed.RateName,
            BaseAmount = total - extra,
            ExtraAmount = extra,
            Total = total,
            PaymentMethod = paymentMethod,
            TenderedAmount = tendered,
            ChangeAmount = paymentMethod == PaymentMethods.Cash && tendered is { } t ? t - total : null,
            Reference = reference,
            CashierName = cashierName
        };
    }
}
