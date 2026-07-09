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

    /// <summary>Parte pagada en efectivo cuando la forma de pago es mixta.</summary>
    public decimal? CashPortion { get; init; }

    /// <summary>Parte pagada por SINPE cuando la forma de pago es mixta.</summary>
    public decimal? SinpePortion { get; init; }

    /// <summary>Marca el comprobante como reimpresión (el cliente ya había pagado).</summary>
    public bool IsReprint { get; init; }

    public static ExitReceipt FromClosedSession(
        ParkingSession closed,
        string paymentMethod,
        decimal? tendered,
        string? reference,
        string cashierName,
        decimal? cashPortion = null,
        decimal? sinpePortion = null)
    {
        var total = closed.ChargedAmount ?? 0m;
        var extra = closed.ExtraAmount ?? 0m;
        return new ExitReceipt
        {
            Plate = closed.Plate,
            EntryAt = closed.EntryAt,
            ExitAt = closed.ExitAt ?? DateTime.Now,
            RateName = DescribeRate(closed),
            BaseAmount = total - extra,
            ExtraAmount = extra,
            Total = total,
            PaymentMethod = paymentMethod,
            TenderedAmount = tendered,
            ChangeAmount = paymentMethod == PaymentMethods.Cash && tendered is { } t ? t - total : null,
            Reference = reference,
            CashierName = cashierName,
            CashPortion = cashPortion,
            SinpePortion = sinpePortion
        };
    }

    /// <summary>Reconstruye el comprobante desde el pago guardado, para reimprimirlo.</summary>
    public static ExitReceipt FromPayment(ParkingSession session, Payment payment)
    {
        var extra = session.ExtraAmount ?? 0m;
        return new ExitReceipt
        {
            Plate = session.Plate,
            EntryAt = session.EntryAt,
            ExitAt = session.ExitAt ?? payment.PaidAt,
            RateName = DescribeRate(session),
            BaseAmount = payment.Amount - extra,
            ExtraAmount = extra,
            Total = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TenderedAmount = payment.TenderedAmount,
            ChangeAmount = payment.ChangeAmount,
            Reference = payment.Reference,
            CashierName = payment.Username,
            CashPortion = payment.CashAmount,
            SinpePortion = payment.SinpeAmount,
            IsReprint = true
        };
    }

    /// <summary>Nombre de la tarifa con que se cobró: por día (con la cantidad), personalizada o la asignada.</summary>
    public static string DescribeRate(ParkingSession session) =>
        session.ChargedDays is { } days
            ? $"Por día ({days} {(days == 1 ? "día" : "días")})"
            : session.HasCustomRate ? "Personalizada" : session.RateName;
}
