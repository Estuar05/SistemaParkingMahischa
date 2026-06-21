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

    public override string ToString() => $"{Name} - {Amount:C0}";
}

