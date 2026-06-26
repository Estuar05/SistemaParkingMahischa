namespace SistemaParkingMahischa.Models;

public sealed class ParkingSession
{
    public long SessionId { get; set; }
    public Guid TicketCode { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string PlateNormalized { get; set; } = string.Empty;
    public DateTime EntryAt { get; set; }
    public DateTime? ExitAt { get; set; }
    public int RateId { get; set; }
    public string RateName { get; set; } = string.Empty;
    public string RateType { get; set; } = "Hora";
    public decimal RateAmount { get; set; }
    public int GraceMinutes { get; set; }
    public int? BlockMinutes { get; set; }
    public decimal? BlockAmount { get; set; }
    public string EnteredBy { get; set; } = string.Empty;
    public string? ExitedBy { get; set; }
    public string Status { get; set; } = "A";
    public decimal? ChargedAmount { get; set; }
    public decimal? ExtraAmount { get; set; }

    // Tarifa personalizada para esta estadía (no se guarda como tarifa global).
    public string? CustomRateType { get; set; }
    public decimal? CustomRateAmount { get; set; }
    public int? CustomGraceMinutes { get; set; }
    public int? CustomBlockMinutes { get; set; }
    public decimal? CustomBlockAmount { get; set; }
    public string? CustomNote { get; set; }

    // Forma de pago registrada al cobrar (Efectivo / SINPE).
    public string? PaymentMethod { get; set; }

    public bool HasCustomRate => CustomRateAmount.HasValue && !string.IsNullOrWhiteSpace(CustomRateType);

    // Parámetros de tarifa efectivos: usa la tarifa personalizada si existe, si no la tarifa asignada.
    public string EffectiveRateType => HasCustomRate ? CustomRateType! : RateType;
    public decimal EffectiveRateAmount => HasCustomRate ? CustomRateAmount!.Value : RateAmount;
    public int EffectiveGraceMinutes => HasCustomRate ? (CustomGraceMinutes ?? 0) : GraceMinutes;
    public int? EffectiveBlockMinutes => HasCustomRate ? CustomBlockMinutes : BlockMinutes;
    public decimal? EffectiveBlockAmount => HasCustomRate ? CustomBlockAmount : BlockAmount;

    public TimeSpan CurrentDuration => (ExitAt ?? DateTime.Now) - EntryAt;
}

