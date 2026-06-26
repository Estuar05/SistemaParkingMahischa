namespace SistemaParkingMahischa.Models;

public sealed class ParkingRate
{
    public int RateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RateType { get; set; } = "Hora";
    public decimal Amount { get; set; }
    public int GraceMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    /// <summary>
    /// Tope por bloque para tarifas por hora: cada <see cref="BlockMinutes"/> minutos no se cobra
    /// más de <see cref="BlockAmount"/>. Ej: ₡700/hora con tope de ₡3000 por cada 12h (720 min).
    /// </summary>
    public int? BlockMinutes { get; set; }
    public decimal? BlockAmount { get; set; }

    public override string ToString() => $"{Name} - {Amount:C0}";
}

