namespace SistemaParkingMahischa.Models;

public sealed class DashboardStats
{
    public int ActiveVehicles { get; set; }
    public int ExitsToday { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueCurrentUserToday { get; set; }
}

