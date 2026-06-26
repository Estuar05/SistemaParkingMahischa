using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Controllers;

public sealed class ParkingController
{
    private readonly ParkingService _parkingService = new();
    private readonly RateService _rateService = new();
    private readonly AuthService _authService = new();
    private readonly ClosureService _closureService = new();
    private readonly IncomeService _incomeService = new();

    public List<ParkingRate> GetActiveRates() => _rateService.GetRates(activeOnly: true);

    public List<ParkingRate> GetRates() => _rateService.GetRates();

    public void SaveRate(ParkingRate rate, int actingUserId) => _rateService.SaveRate(rate, actingUserId);

    public ParkingSession RegisterEntry(string plate, int rateId, int userId) =>
        _parkingService.RegisterEntry(plate, rateId, userId);

    public ParkingSession? FindByTicket(string ticketCode) => _parkingService.GetSessionByTicketCode(ticketCode);

    public List<ParkingSession> FindByPlate(string plate, bool activeOnly) => _parkingService.SearchByPlate(plate, activeOnly);

    public List<ParkingSession> GetSessions(bool activeOnly = true) => _parkingService.GetSessions(activeOnly);

    public ParkingSession RegisterExit(
        long sessionId,
        int userId,
        decimal extraAmount = 0m,
        string paymentMethod = PaymentMethods.Cash,
        string? reference = null,
        decimal? tenderedAmount = null) =>
        _parkingService.RegisterExit(sessionId, userId, extraAmount, paymentMethod, reference, tenderedAmount);

    public void SetCustomRate(long sessionId, string rateType, decimal amount, int graceMinutes, int? blockMinutes, decimal? blockAmount, string? note, int userId) =>
        _parkingService.SetCustomRate(sessionId, rateType, amount, graceMinutes, blockMinutes, blockAmount, note, userId);

    public ParkingSession? GetSession(long sessionId) => _parkingService.GetSessionById(sessionId);

    public DashboardStats GetStats(int userId) => _parkingService.GetDashboardStats(userId);

    public List<User> GetUsers() => _authService.GetUsers();

    public void SaveUser(
        int? userId,
        string identificationNumber,
        string fullName,
        string roleName,
        string? password,
        bool isActive,
        IEnumerable<string> permissions,
        int actingUserId) =>
        _authService.SaveUser(userId, identificationNumber, fullName, roleName, password, isActive, permissions, actingUserId);

    public decimal GetExpectedForUser(int userId, DateTime fromAt, DateTime toAt) =>
        _closureService.GetExpectedForUser(userId, fromAt, toAt);

    public IncomeSummary GetUserTotals(int userId, DateTime fromAt, DateTime toAt) =>
        _closureService.GetUserTotals(userId, fromAt, toAt);

    public EmployeeClosure CreateEmployeeClosure(int userId, DateTime fromAt, DateTime toAt, IReadOnlyDictionary<decimal, int> denominations, int createdByUserId) =>
        _closureService.CreateEmployeeClosure(userId, fromAt, toAt, denominations, createdByUserId);

    public decimal GetSystemCashForDate(DateTime date) => _closureService.GetSystemCashForDate(date);

    public IncomeSummary GetSummaryForDate(DateTime date) => _closureService.GetSummaryForDate(date);

    public List<IncomeRecord> GetIncome(DateTime fromAt, DateTime toAt, string? paymentMethod = null, int? userId = null) =>
        _incomeService.GetIncome(fromAt, toAt, paymentMethod, userId);

    public IncomeSummary GetIncomeSummary(DateTime fromAt, DateTime toAt, int? userId = null) =>
        _incomeService.GetSummary(fromAt, toAt, userId);

    public long CreateCashClosure(DateTime date, IReadOnlyDictionary<decimal, int> denominations, int createdByUserId) =>
        _closureService.CreateCashClosure(date, denominations, createdByUserId);

    public List<ClosureHistoryRecord> GetEmployeeClosureHistory(DateTime fromDate, DateTime toDate) =>
        _closureService.GetEmployeeClosureHistory(fromDate, toDate);

    public List<ClosureHistoryRecord> GetCashClosureHistory(DateTime fromDate, DateTime toDate) =>
        _closureService.GetCashClosureHistory(fromDate, toDate);
}
