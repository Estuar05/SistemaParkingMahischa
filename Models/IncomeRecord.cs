namespace SistemaParkingMahischa.Models;

public sealed class IncomeRecord
{
    public long PaymentId { get; set; }
    public DateTime PaidAt { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string RateName { get; set; } = string.Empty;
    public bool IsCustom { get; set; }

    /// <summary>Cantidad de días cobrados cuando la salida se cobró por día.</summary>
    public int? ChargedDays { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = PaymentMethods.Cash;
    public string? Reference { get; set; }
    public string Username { get; set; } = string.Empty;

    /// <summary>Desglose del pago mixto (efectivo / SINPE); nulo en pagos anteriores a esta función.</summary>
    public decimal? CashAmount { get; set; }
    public decimal? SinpeAmount { get; set; }
}

/// <summary>Totales de ingresos desglosados por forma de pago.</summary>
public sealed class IncomeSummary
{
    public decimal Cash { get; set; }
    public decimal Sinpe { get; set; }
    public int Count { get; set; }

    public decimal Total => Cash + Sinpe;
}
