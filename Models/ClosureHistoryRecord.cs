namespace SistemaParkingMahischa.Models;

public sealed class ClosureHistoryRecord
{
    public string ClosureType { get; set; } = string.Empty;
    public long ClosureId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime FromAt { get; set; }
    public DateTime ToAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal DeliveredAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public decimal MinimumCashAmount { get; set; }
    public decimal SystemAmount { get; set; }
    public decimal CountedAmount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal SinpeAmount { get; set; }
    public List<CashDenominationDetail> Denominations { get; set; } = [];

    public string DisplayName => ClosureType == "Caja"
        ? $"Cierre de caja #{ClosureId}"
        : $"Cierre de empleado #{ClosureId}";
}

public sealed class CashDenominationDetail
{
    public decimal Denomination { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount => Denomination * Quantity;
}
