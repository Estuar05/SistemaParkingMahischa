using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class ParkingService
{
    public ParkingSession RegisterEntry(string plate, int rateId, int userId)
    {
        var normalizedPlate = PlateHelper.Normalize(plate);
        if (normalizedPlate.Length < 3)
        {
            throw new InvalidOperationException("La placa debe tener al menos 3 caracteres.");
        }

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            using (var exists = connection.CreateCommand())
            {
                exists.Transaction = transaction;
                exists.CommandText = """
                    SELECT TOP (1) SessionId
                    FROM dbo.ParkingSessions WITH (UPDLOCK, HOLDLOCK)
                    WHERE PlateNormalized = @PlateNormalized AND Status = 'A';
                    """;
                exists.Parameters.AddWithValue("@PlateNormalized", normalizedPlate);
                if (exists.ExecuteScalar() is not null)
                {
                    throw new InvalidOperationException("Este vehículo ya tiene una entrada activa.");
                }
            }

            long sessionId;
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.ParkingSessions(Plate, PlateNormalized, RateId, EnteredByUserId)
                    OUTPUT INSERTED.SessionId
                    VALUES (@Plate, @PlateNormalized, @RateId, @UserId);
                    """;
                insert.Parameters.AddWithValue("@Plate", plate.Trim().ToUpperInvariant());
                insert.Parameters.AddWithValue("@PlateNormalized", normalizedPlate);
                insert.Parameters.AddWithValue("@RateId", rateId);
                insert.Parameters.AddWithValue("@UserId", userId);
                sessionId = Convert.ToInt64(insert.ExecuteScalar());
            }

            transaction.Commit();
            AuditService.Log(userId, "RegistrarEntrada", "ParkingSessions", sessionId.ToString(), $"Placa {normalizedPlate}");
            return GetSessionById(sessionId) ?? throw new InvalidOperationException("No se pudo leer la entrada registrada.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public ParkingSession? GetSessionByTicketCode(string ticketCode)
    {
        if (!Guid.TryParse(ticketCode.Trim(), out var code))
        {
            return null;
        }

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = SessionSelectSql + " WHERE s.TicketCode = @TicketCode;";
        command.Parameters.AddWithValue("@TicketCode", code);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapSession(reader) : null;
    }

    public ParkingSession? GetSessionById(long sessionId)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = SessionSelectSql + " WHERE s.SessionId = @SessionId;";
        command.Parameters.AddWithValue("@SessionId", sessionId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapSession(reader) : null;
    }

    public List<ParkingSession> SearchByPlate(string plate, bool activeOnly)
    {
        var normalized = PlateHelper.Normalize(plate);
        var sessions = new List<ParkingSession>();

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = SessionSelectSql + Environment.NewLine + """
            WHERE (@ActiveOnly = 0 OR s.Status = 'A')
              AND (@Plate = '' OR s.PlateNormalized LIKE @Plate + '%')
            ORDER BY s.EntryAt DESC;
            """;
        command.Parameters.AddWithValue("@ActiveOnly", activeOnly);
        command.Parameters.AddWithValue("@Plate", normalized);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(MapSession(reader));
        }

        return sessions;
    }

    public List<ParkingSession> GetSessions(bool activeOnly = true, int top = 100)
    {
        var sessions = new List<ParkingSession>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = SessionSelectSql + Environment.NewLine + """
            WHERE (@ActiveOnly = 0 OR s.Status = 'A')
            ORDER BY s.EntryAt DESC;
            """;
        command.Parameters.AddWithValue("@ActiveOnly", activeOnly);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            sessions.Add(MapSession(reader));
        }

        return sessions;
    }

    public ParkingSession RegisterExit(long sessionId, int userId)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            ParkingSession session;
            using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = SessionSelectSql.Replace(
                    "FROM dbo.ParkingSessions s",
                    "FROM dbo.ParkingSessions s WITH (UPDLOCK, ROWLOCK)") + " WHERE s.SessionId = @SessionId;";
                select.Parameters.AddWithValue("@SessionId", sessionId);
                using var reader = select.ExecuteReader();
                if (!reader.Read())
                {
                    throw new InvalidOperationException("No se encontró el registro.");
                }

                session = MapSession(reader);
            }

            if (session.Status != "A")
            {
                throw new InvalidOperationException("Este vehículo ya tiene salida registrada.");
            }

            var exitAt = DateTime.Now;
            var amount = CalculateAmount(session.EntryAt, exitAt, session.RateType, session.RateAmount, session.GraceMinutes);

            using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE dbo.ParkingSessions
                    SET ExitAt = @ExitAt,
                        ExitedByUserId = @UserId,
                        ChargedAmount = @Amount,
                        Status = 'C'
                    WHERE SessionId = @SessionId AND Status = 'A';
                    """;
                update.Parameters.AddWithValue("@ExitAt", exitAt);
                update.Parameters.AddWithValue("@UserId", userId);
                update.Parameters.AddWithValue("@Amount", amount);
                update.Parameters.AddWithValue("@SessionId", sessionId);
                if (update.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("No se pudo cerrar la salida.");
                }
            }

            using (var payment = connection.CreateCommand())
            {
                payment.Transaction = transaction;
                payment.CommandText = """
                    INSERT INTO dbo.Payments(SessionId, Amount, PaidAt, UserId, PaymentMethod)
                    VALUES (@SessionId, @Amount, @PaidAt, @UserId, N'Efectivo');
                    """;
                payment.Parameters.AddWithValue("@SessionId", sessionId);
                payment.Parameters.AddWithValue("@Amount", amount);
                payment.Parameters.AddWithValue("@PaidAt", exitAt);
                payment.Parameters.AddWithValue("@UserId", userId);
                payment.ExecuteNonQuery();
            }

            transaction.Commit();
            AuditService.Log(userId, "RegistrarSalida", "ParkingSessions", sessionId.ToString(), $"Placa {session.Plate}, cobro {amount:0.00}");
            return GetSessionById(sessionId) ?? throw new InvalidOperationException("No se pudo leer la salida registrada.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public DashboardStats GetDashboardStats(int currentUserId)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ActiveVehicles = (SELECT COUNT(1) FROM dbo.ParkingSessions WHERE Status = 'A'),
                ExitsToday = (SELECT COUNT(1) FROM dbo.ParkingSessions WHERE Status = 'C' AND CONVERT(date, ExitAt) = CONVERT(date, SYSDATETIME())),
                RevenueToday = (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE CONVERT(date, PaidAt) = CONVERT(date, SYSDATETIME())),
                RevenueCurrentUserToday = (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE UserId = @UserId AND CONVERT(date, PaidAt) = CONVERT(date, SYSDATETIME()));
            """;
        command.Parameters.AddWithValue("@UserId", currentUserId);

        using var reader = command.ExecuteReader();
        reader.Read();
        return new DashboardStats
        {
            ActiveVehicles = reader.GetInt32(reader.GetOrdinal("ActiveVehicles")),
            ExitsToday = reader.GetInt32(reader.GetOrdinal("ExitsToday")),
            RevenueToday = reader.GetDecimal(reader.GetOrdinal("RevenueToday")),
            RevenueCurrentUserToday = reader.GetDecimal(reader.GetOrdinal("RevenueCurrentUserToday"))
        };
    }

    public static decimal CalculateAmount(DateTime entryAt, DateTime exitAt, string rateType, decimal amount, int graceMinutes)
    {
        var minutes = Math.Max(0, (exitAt - entryAt).TotalMinutes);
        if (minutes <= graceMinutes)
        {
            return 0m;
        }

        var billableMinutes = Math.Max(1, minutes - graceMinutes);
        var units = rateType switch
        {
            "Hora" => Math.Ceiling(billableMinutes / 60d),
            "Dia" => Math.Ceiling(billableMinutes / 1440d),
            "Semana" => Math.Ceiling(billableMinutes / 10080d),
            "Mes" => Math.Ceiling(billableMinutes / 43200d),
            _ => 1d
        };

        return amount * Convert.ToDecimal(units);
    }

    private const string SessionSelectSql = """
        SELECT
            s.SessionId,
            s.TicketCode,
            s.Plate,
            s.PlateNormalized,
            s.EntryAt,
            s.ExitAt,
            s.RateId,
            r.RateName,
            r.RateType,
            r.Amount,
            r.GraceMinutes,
            entered.FullName AS EnteredBy,
            exited.FullName AS ExitedBy,
            s.Status,
            s.ChargedAmount
        FROM dbo.ParkingSessions s
        INNER JOIN dbo.ParkingRates r ON r.RateId = s.RateId
        INNER JOIN dbo.Users entered ON entered.UserId = s.EnteredByUserId
        LEFT JOIN dbo.Users exited ON exited.UserId = s.ExitedByUserId
        """;

    private static ParkingSession MapSession(SqlDataReader reader) => new()
    {
        SessionId = reader.GetInt64(reader.GetOrdinal("SessionId")),
        TicketCode = reader.GetGuid(reader.GetOrdinal("TicketCode")),
        Plate = reader.GetString(reader.GetOrdinal("Plate")),
        PlateNormalized = reader.GetString(reader.GetOrdinal("PlateNormalized")),
        EntryAt = reader.GetDateTime(reader.GetOrdinal("EntryAt")),
        ExitAt = reader.IsDBNull(reader.GetOrdinal("ExitAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ExitAt")),
        RateId = reader.GetInt32(reader.GetOrdinal("RateId")),
        RateName = reader.GetString(reader.GetOrdinal("RateName")),
        RateType = reader.GetString(reader.GetOrdinal("RateType")),
        RateAmount = reader.GetDecimal(reader.GetOrdinal("Amount")),
        GraceMinutes = reader.GetInt32(reader.GetOrdinal("GraceMinutes")),
        EnteredBy = reader.GetString(reader.GetOrdinal("EnteredBy")),
        ExitedBy = reader.IsDBNull(reader.GetOrdinal("ExitedBy")) ? null : reader.GetString(reader.GetOrdinal("ExitedBy")),
        Status = reader.GetString(reader.GetOrdinal("Status")),
        ChargedAmount = reader.IsDBNull(reader.GetOrdinal("ChargedAmount")) ? null : reader.GetDecimal(reader.GetOrdinal("ChargedAmount"))
    };
}
