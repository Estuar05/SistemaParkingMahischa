namespace SistemaParkingMahischa.Models;

public sealed class EmployeeClosure
{
    public long ClosureId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime FromAt { get; set; }
    public DateTime ToAt { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

