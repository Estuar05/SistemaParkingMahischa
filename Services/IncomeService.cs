using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

/// <summary>
/// Registro de ingresos: cada cobro registrado al dar salida es un ingreso. Sobre estos datos
/// trabajan los cierres (de caja y de empleado).
/// </summary>
public sealed class IncomeService
{
    public List<IncomeRecord> GetIncome(DateTime fromAt, DateTime toAt, string? paymentMethod = null, int? userId = null)
    {
        var records = new List<IncomeRecord>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                p.PaymentId,
                p.PaidAt,
                s.Plate,
                r.RateName,
                CASE WHEN s.CustomRateAmount IS NOT NULL THEN 1 ELSE 0 END AS IsCustom,
                p.Amount,
                p.PaymentMethod,
                p.Reference,
                u.FullName AS Username
            FROM dbo.Payments p
            INNER JOIN dbo.ParkingSessions s ON s.SessionId = p.SessionId
            INNER JOIN dbo.ParkingRates r ON r.RateId = s.RateId
            INNER JOIN dbo.Users u ON u.UserId = p.UserId
            WHERE p.PaidAt >= @FromAt AND p.PaidAt <= @ToAt
              AND (@Method IS NULL OR p.PaymentMethod = @Method)
              AND (@UserId IS NULL OR p.UserId = @UserId)
            ORDER BY p.PaidAt DESC;
            """;
        command.Parameters.AddWithValue("@FromAt", fromAt);
        command.Parameters.AddWithValue("@ToAt", toAt);
        command.Parameters.AddWithValue("@Method", (object?)paymentMethod ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new IncomeRecord
            {
                PaymentId = reader.GetInt64(reader.GetOrdinal("PaymentId")),
                PaidAt = reader.GetDateTime(reader.GetOrdinal("PaidAt")),
                Plate = reader.GetString(reader.GetOrdinal("Plate")),
                RateName = reader.GetString(reader.GetOrdinal("RateName")),
                IsCustom = reader.GetInt32(reader.GetOrdinal("IsCustom")) == 1,
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod")),
                Reference = reader.IsDBNull(reader.GetOrdinal("Reference")) ? null : reader.GetString(reader.GetOrdinal("Reference")),
                Username = reader.GetString(reader.GetOrdinal("Username"))
            });
        }

        return records;
    }

    /// <summary>Totales por forma de pago en un rango de fechas (opcionalmente de un empleado).</summary>
    public IncomeSummary GetSummary(DateTime fromAt, DateTime toAt, int? userId = null)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Cash = COALESCE(SUM(CASE WHEN PaymentMethod = N'Efectivo' THEN Amount ELSE 0 END), 0),
                Sinpe = COALESCE(SUM(CASE WHEN PaymentMethod = N'SINPE' THEN Amount ELSE 0 END), 0),
                Cnt = COUNT(1)
            FROM dbo.Payments
            WHERE PaidAt >= @FromAt AND PaidAt <= @ToAt
              AND (@UserId IS NULL OR UserId = @UserId);
            """;
        command.Parameters.AddWithValue("@FromAt", fromAt);
        command.Parameters.AddWithValue("@ToAt", toAt);
        command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

        using var reader = command.ExecuteReader();
        reader.Read();
        return new IncomeSummary
        {
            Cash = reader.GetDecimal(reader.GetOrdinal("Cash")),
            Sinpe = reader.GetDecimal(reader.GetOrdinal("Sinpe")),
            Count = reader.GetInt32(reader.GetOrdinal("Cnt"))
        };
    }

    /// <summary>Totales por forma de pago para un día calendario (usado por el cierre de caja).</summary>
    public IncomeSummary GetSummaryForDate(DateTime date)
    {
        var from = date.Date;
        var to = date.Date.AddDays(1).AddSeconds(-1);
        return GetSummary(from, to);
    }
}
