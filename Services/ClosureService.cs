using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Models;
using Microsoft.Data.SqlClient;

namespace SistemaParkingMahischa.Services;

public sealed class ClosureService
{
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
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            });
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

    public List<CashDenominationDetail> GetCashDenominations(long cashClosureId)
    {
        var details = new List<CashDenominationDetail>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Denomination, Quantity
            FROM dbo.CashClosureDenominations
            WHERE CashClosureId = @CashClosureId
            ORDER BY Denomination DESC;
            """;
        command.Parameters.AddWithValue("@CashClosureId", cashClosureId);

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

    public decimal GetExpectedForUser(int userId, DateTime fromAt, DateTime toAt)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(Amount), 0)
            FROM dbo.Payments
            WHERE UserId = @UserId AND PaidAt >= @FromAt AND PaidAt <= @ToAt;
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@FromAt", fromAt);
        command.Parameters.AddWithValue("@ToAt", toAt);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public EmployeeClosure CreateEmployeeClosure(int userId, DateTime fromAt, DateTime toAt, decimal deliveredAmount, int createdByUserId)
    {
        var expected = GetExpectedForUser(userId, fromAt, toAt);
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using (var duplicate = connection.CreateCommand())
        {
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

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.EmployeeClosures(UserId, FromAt, ToAt, ExpectedAmount, DeliveredAmount, CreatedByUserId)
            OUTPUT INSERTED.EmployeeClosureId, INSERTED.CreatedAt
            VALUES (@UserId, @FromAt, @ToAt, @ExpectedAmount, @DeliveredAmount, @CreatedByUserId);
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@FromAt", fromAt);
        command.Parameters.AddWithValue("@ToAt", toAt);
        command.Parameters.AddWithValue("@ExpectedAmount", expected);
        command.Parameters.AddWithValue("@DeliveredAmount", deliveredAmount);
        command.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);

        using var reader = command.ExecuteReader();
        reader.Read();
        var closure = new EmployeeClosure
        {
            ClosureId = reader.GetInt64(0),
            UserId = userId,
            FromAt = fromAt,
            ToAt = toAt,
            ExpectedAmount = expected,
            DeliveredAmount = deliveredAmount,
            DifferenceAmount = deliveredAmount - expected,
            CreatedAt = reader.GetDateTime(1)
        };
        reader.Close();
        AuditService.Log(createdByUserId, "CierreEmpleado", "EmployeeClosures", closure.ClosureId.ToString(),
            $"Empleado {userId}, esperado {expected:0.00}, entregado {deliveredAmount:0.00}");
        return closure;
    }

    public decimal GetSystemCashForDate(DateTime date)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(Amount), 0)
            FROM dbo.Payments
            WHERE CONVERT(date, PaidAt) = @ClosureDate;
            """;
        command.Parameters.AddWithValue("@ClosureDate", date.Date);
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public long CreateCashClosure(DateTime date, IReadOnlyDictionary<decimal, int> denominations, int createdByUserId)
    {
        var countedAmount = denominations.Sum(item => item.Key * item.Value);
        var systemAmount = GetSystemCashForDate(date);

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
                    INSERT INTO dbo.CashClosures(ClosureDate, MinimumCashAmount, SystemAmount, CountedAmount, CreatedByUserId)
                    OUTPUT INSERTED.CashClosureId
                    VALUES (@ClosureDate, @MinimumCashAmount, @SystemAmount, @CountedAmount, @CreatedByUserId);
                    """;
                command.Parameters.AddWithValue("@ClosureDate", date.Date);
                command.Parameters.AddWithValue("@MinimumCashAmount", AppSettings.MinimumCashAmount);
                command.Parameters.AddWithValue("@SystemAmount", systemAmount);
                command.Parameters.AddWithValue("@CountedAmount", countedAmount);
                command.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                closureId = Convert.ToInt64(command.ExecuteScalar());
            }

            foreach (var item in denominations.Where(item => item.Value > 0))
            {
                using var detail = connection.CreateCommand();
                detail.Transaction = transaction;
                detail.CommandText = """
                    INSERT INTO dbo.CashClosureDenominations(CashClosureId, Denomination, Quantity)
                    VALUES (@CashClosureId, @Denomination, @Quantity);
                    """;
                detail.Parameters.AddWithValue("@CashClosureId", closureId);
                detail.Parameters.AddWithValue("@Denomination", item.Key);
                detail.Parameters.AddWithValue("@Quantity", item.Value);
                detail.ExecuteNonQuery();
            }

            transaction.Commit();
            AuditService.Log(createdByUserId, "CierreCaja", "CashClosures", closureId.ToString(),
                $"Sistema {systemAmount:0.00}, contado {countedAmount:0.00}");
            return closureId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
