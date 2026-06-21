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
    public string EnteredBy { get; set; } = string.Empty;
    public string? ExitedBy { get; set; }
    public string Status { get; set; } = "A";
    public decimal? ChargedAmount { get; set; }

    public TimeSpan CurrentDuration => (ExitAt ?? DateTime.Now) - EntryAt;
}

