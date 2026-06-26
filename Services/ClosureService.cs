using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Models;
using Microsoft.Data.SqlClient;

namespace SistemaParkingMahischa.Services;

public sealed class ClosureService
{
    private readonly IncomeService _incomeService = new();

    public List<ClosureHistoryRecord> GetEmployeeClosureHistory(DateTime fromDate, DateTime toDate)
    {
        var records = new List<ClosureHistoryRecord>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ec.EmployeeClosureId,
                ec.UserId,
                employee.FullName AS EmployeeName,
                createdBy.FullName AS CreatedByName,
                ec.FromAt,
                ec.ToAt,
                ec.ExpectedAmount,
                ec.DeliveredAmount,
                ec.DifferenceAmount,
                ec.CashExpected,
                ec.SinpeExpected,
                ec.CreatedAt
            FROM dbo.EmployeeClosures ec
            INNER JOIN dbo.Users employee ON employee.UserId = ec.UserId
            INNER JOIN dbo.Users createdBy ON createdBy.UserId = ec.CreatedByUserId
            WHERE ec.CreatedAt >= @FromDate AND ec.CreatedAt < DATEADD(day, 1, @ToDate)
            ORDER BY ec.CreatedAt DESC;
            """;
        command.Parameters.AddWithValue("@FromDate", fromDate.Date);
        command.Parameters.AddWithValue("@ToDate", toDate.Date);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new ClosureHistoryRecord
            {
                ClosureType = "Empleado",
                ClosureId = reader.GetInt64(reader.GetOrdinal("EmployeeClosureId")),
                EmployeeName = reader.GetString(reader.GetOrdinal("EmployeeName")),
                CreatedByName = reader.GetString(reader.GetOrdinal("CreatedByName")),
                FromAt = reader.GetDateTime(reader.GetOrdinal("FromAt")),
                ToAt = reader.GetDateTime(reader.GetOrdinal("ToAt")),
                ExpectedAmount = reader.GetDecimal(reader.GetOrdinal("ExpectedAmount")),
                DeliveredAmount = reader.GetDecimal(reader.GetOrdinal("DeliveredAmount")),
                DifferenceAmount = reader.GetDecimal(reader.GetOrdinal("DifferenceAmount")),
                CashAmount = reader.GetDecimal(reader.GetOrdinal("CashExpected")),
                SinpeAmount = reader.GetDecimal(reader.GetOrdinal("SinpeExpected")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        foreach (var record in records)
        {
            record.Denominations = GetEmployeeDenominations(record.ClosureId);
        }

        return records;
    }

    public List<ClosureHistoryRecord> GetCashClosureHistory(DateTime fromDate, DateTime toDate)
    {
        var records = new List<ClosureHistoryRecord>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                cc.CashClosureId,
                cc.ClosureDate,
                cc.MinimumCashAmount,
                cc.SystemAmount,
                cc.SinpeAmount,
                cc.CountedAmount,
                cc.DifferenceAmount,
                cc.CreatedAt,
                createdBy.FullName AS CreatedByName
            FROM dbo.CashClosures cc
            INNER JOIN dbo.Users createdBy ON createdBy.UserId = cc.CreatedByUserId
            WHERE cc.CreatedAt >= @FromDate AND cc.CreatedAt < DATEADD(day, 1, @ToDate)
            ORDER BY cc.CreatedAt DESC;
            """;
        command.Parameters.AddWithValue("@FromDate", fromDate.Date);
        command.Parameters.AddWithValue("@ToDate", toDate.Date);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new ClosureHistoryRecord
            {
                ClosureType = "Caja",
                ClosureId = reader.GetInt64(reader.GetOrdinal("CashClosureId")),
                CreatedByName = reader.GetString(reader.GetOrdinal("CreatedByName")),
                FromAt = reader.GetDateTime(reader.GetOrdinal("ClosureDate")),
                ToAt = reader.GetDateTime(reader.GetOrdinal("ClosureDate")),
                MinimumCashAmount = reader.GetDecimal(reader.GetOrdinal("MinimumCashAmount")),
                SystemAmount = reader.GetDecimal(reader.GetOrdinal("SystemAmount")),
                CashAmount = reader.GetDecimal(reader.GetOrdinal("SystemAmount")),
                SinpeAmount = reader.GetDecimal(reader.GetOrdinal("SinpeAmount")),
                CountedAmount = reader.GetDecimal(reader.GetOrdinal("CountedAmount")),
                DifferenceAmount = reader.GetDecimal(reader.GetOrdinal("DifferenceAmount")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
        }

        foreach (var record in records)
        {
            record.Denominations = GetCashDenominations(record.ClosureId);
        }

        return records;
    }

    public List<CashDenominationDetail> GetCashDenominations(long cashClosureId) =>
        GetDenominations("dbo.CashClosureDenominations", "CashClosureId", cashClosureId);

    public List<CashDenominationDetail> GetEmployeeDenominations(long employeeClosureId) =>
        GetDenominations("dbo.EmployeeClosureDenominations", "EmployeeClosureId", employeeClosureId);

    private static List<CashDenominationDetail> GetDenominations(string table, string keyColumn, long keyValue)
    {
        var details = new List<CashDenominationDetail>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT Denomination, Quantity
            FROM {table}
            WHERE {keyColumn} = @Key
            ORDER BY Denomination DESC;
            """;
        command.Parameters.AddWithValue("@Key", keyValue);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            details.Add(new CashDenominationDetail
            {
                Denomination = reader.GetDecimal(reader.GetOrdinal("Denomination")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity"))
            });
        }

        return details;
    }

    /// <summary>Totales (efectivo / SINPE) cobrados por un empleado en el periodo.</summary>
    public IncomeSummary GetUserTotals(int userId, DateTime fromAt, DateTime toAt) =>
        _incomeService.GetSummary(fromAt, toAt, userId);

    public decimal GetExpectedForUser(int userId, DateTime fromAt, DateTime toAt) =>
        _incomeService.GetSummary(fromAt, toAt, userId).Cash;

    public EmployeeClosure CreateEmployeeClosure(
        int userId,
        DateTime fromAt,
        DateTime toAt,
        IReadOnlyDictionary<decimal, int> denominations,
        int createdByUserId)
    {
        var totals = _incomeService.GetSummary(fromAt, toAt, userId);
        var cashExpected = totals.Cash;
        var deliveredAmount = denominations.Sum(item => item.Key * item.Value);

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var duplicate = connection.CreateCommand())
            {
                duplicate.Transaction = transaction;
                duplicate.CommandText = """
                    SELECT COUNT(1)
                    FROM dbo.EmployeeClosures
                    WHERE UserId = @UserId
                      AND CreatedByUserId = @CreatedByUserId
                      AND CreatedAt >= DATEADD(second, -5, SYSDATETIME());
                    """;
                duplicate.Parameters.AddWithValue("@UserId", userId);
                duplicate.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                if (Convert.ToInt32(duplicate.ExecuteScalar()) > 0)
                {
                    throw new InvalidOperationException("Ya se registro un cierre de empleado hace pocos segundos. Espere antes de intentar de nuevo.");
                }
            }

            long closureId;
            DateTime createdAt;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO dbo.EmployeeClosures(UserId, FromAt, ToAt, ExpectedAmount, DeliveredAmount, CashExpected, SinpeExpected, CreatedByUserId)
                    OUTPUT INSERTED.EmployeeClosureId, INSERTED.CreatedAt
                    VALUES (@UserId, @FromAt, @ToAt, @ExpectedAmount, @DeliveredAmount, @CashExpected, @SinpeExpected, @CreatedByUserId);
                    """;
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@FromAt", fromAt);
                command.Parameters.AddWithValue("@ToAt", toAt);
                command.Parameters.AddWithValue("@ExpectedAmount", cashExpected);
                command.Parameters.AddWithValue("@DeliveredAmount", deliveredAmount);
                command.Parameters.AddWithValue("@CashExpected", cashExpected);
                command.Parameters.AddWithValue("@SinpeExpected", totals.Sinpe);
                command.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                using var reader = command.ExecuteReader();
                reader.Read();
                closureId = reader.GetInt64(0);
                createdAt = reader.GetDateTime(1);
            }

            InsertDenominations(connection, transaction, "dbo.EmployeeClosureDenominations", "EmployeeClosureId", closureId, denominations);

            transaction.Commit();
            AuditService.Log(createdByUserId, "CierreEmpleado", "EmployeeClosures", closureId.ToString(),
                $"Empleado {userId}, efectivo esperado {cashExpected:0.00}, entregado {deliveredAmount:0.00}, SINPE {totals.Sinpe:0.00}");

            return new EmployeeClosure
            {
                ClosureId = closureId,
                UserId = userId,
                FromAt = fromAt,
                ToAt = toAt,
                ExpectedAmount = cashExpected,
                DeliveredAmount = deliveredAmount,
                DifferenceAmount = deliveredAmount - cashExpected,
                CashExpected = cashExpected,
                SinpeExpected = totals.Sinpe,
                CreatedAt = createdAt
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public decimal GetSystemCashForDate(DateTime date) => _incomeService.GetSummaryForDate(date).Cash;

    public IncomeSummary GetSummaryForDate(DateTime date) => _incomeService.GetSummaryForDate(date);

    public long CreateCashClosure(DateTime date, IReadOnlyDictionary<decimal, int> denominations, int createdByUserId)
    {
        var countedAmount = denominations.Sum(item => item.Key * item.Value);
        var totals = _incomeService.GetSummaryForDate(date);

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var duplicate = connection.CreateCommand())
            {
                duplicate.Transaction = transaction;
                duplicate.CommandText = """
                    SELECT COUNT(1)
                    FROM dbo.CashClosures
                    WHERE CreatedByUserId = @CreatedByUserId
                      AND CreatedAt >= DATEADD(second, -5, SYSDATETIME());
                    """;
                duplicate.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                if (Convert.ToInt32(duplicate.ExecuteScalar()) > 0)
                {
                    throw new InvalidOperationException("Ya se registro un cierre de caja hace pocos segundos. Espere antes de intentar de nuevo.");
                }
            }

            long closureId;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO dbo.CashClosures(ClosureDate, MinimumCashAmount, SystemAmount, SinpeAmount, CountedAmount, CreatedByUserId)
                    OUTPUT INSERTED.CashClosureId
                    VALUES (@ClosureDate, @MinimumCashAmount, @SystemAmount, @SinpeAmount, @CountedAmount, @CreatedByUserId);
                    """;
                command.Parameters.AddWithValue("@ClosureDate", date.Date);
                command.Parameters.AddWithValue("@MinimumCashAmount", AppSettings.MinimumCashAmount);
                command.Parameters.AddWithValue("@SystemAmount", totals.Cash);
                command.Parameters.AddWithValue("@SinpeAmount", totals.Sinpe);
                command.Parameters.AddWithValue("@CountedAmount", countedAmount);
                command.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                closureId = Convert.ToInt64(command.ExecuteScalar());
            }

            InsertDenominations(connection, transaction, "dbo.CashClosureDenominations", "CashClosureId", closureId, denominations);

            transaction.Commit();
            AuditService.Log(createdByUserId, "CierreCaja", "CashClosures", closureId.ToString(),
                $"Efectivo {totals.Cash:0.00}, SINPE {totals.Sinpe:0.00}, contado {countedAmount:0.00}");
            return closureId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void InsertDenominations(
        SqlConnection connection,
        SqlTransaction transaction,
        string table,
        string keyColumn,
        long keyValue,
        IReadOnlyDictionary<decimal, int> denominations)
    {
        foreach (var item in denominations.Where(item => item.Value > 0))
        {
            using var detail = connection.CreateCommand();
            detail.Transaction = transaction;
            detail.CommandText = $"""
                INSERT INTO {table}({keyColumn}, Denomination, Quantity)
                VALUES (@Key, @Denomination, @Quantity);
                """;
            detail.Parameters.AddWithValue("@Key", keyValue);
            detail.Parameters.AddWithValue("@Denomination", item.Key);
            detail.Parameters.AddWithValue("@Quantity", item.Value);
            detail.ExecuteNonQuery();
        }
    }
}
