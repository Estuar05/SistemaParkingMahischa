using System.Text.RegularExpressions;
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
        if (ExtractTicketGuid(ticketCode) is not { } code)
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

    /// <summary>
    /// Obtiene el GUID del ticket a partir del texto escaneado, tolerando variaciones del lector:
    /// con o sin guiones, con espacios, o con prefijos/sufijos agregados por el escáner.
    /// </summary>
    private static Guid? ExtractTicketGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();

        // Caso normal: GUID con o sin guiones (formatos "D" o "N").
        if (Guid.TryParse(trimmed, out var direct))
        {
            return direct;
        }

        // El lector pudo agregar prefijo/sufijo: buscar un GUID con guiones dentro del texto.
        var match = Regex.Match(trimmed, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        if (match.Success && Guid.TryParse(match.Value, out var matched))
        {
            return matched;
        }

        // El lector pudo alterar los separadores: reconstruir desde 32 dígitos hexadecimales.
        var hex = new string(trimmed.Where(Uri.IsHexDigit).ToArray());
        if (hex.Length == 32 && Guid.TryParseExact(hex, "N", out var fromHex))
        {
            return fromHex;
        }

        return null;
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

    public ParkingSession RegisterExit(
        long sessionId,
        int userId,
        decimal extraAmount = 0m,
        string paymentMethod = PaymentMethods.Cash,
        string? reference = null,
        decimal? tenderedAmount = null,
        decimal? cashPortion = null,
        decimal? sinpePortion = null,
        decimal? quotedBaseAmount = null,
        int? chargedDays = null,
        DateTime? quotedAt = null)
    {
        if (chargedDays is < 1)
        {
            throw new InvalidOperationException("La cantidad de días debe ser 1 o mayor.");
        }

        if (extraAmount < 0)
        {
            throw new InvalidOperationException("El monto extra no puede ser negativo.");
        }

        if (!PaymentMethods.All.Contains(paymentMethod))
        {
            paymentMethod = PaymentMethods.Cash;
        }

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

            var now = DateTime.Now;
            var paidAt = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
            // El cierre usa el instante en que se abrió la ventana de cobro. Así el tiempo,
            // el monto mostrado y la salida registrada corresponden al mismo momento. Si un
            // llamador antiguo no envía el instante, se conserva el comportamiento anterior.
            var exitAt = quotedAt is { } frozenAt
                && frozenAt >= session.EntryAt
                && frozenAt <= paidAt
                    ? frozenAt.AddTicks(-(frozenAt.Ticks % TimeSpan.TicksPerSecond))
                    : paidAt;
            // El monto por tiempo se congela con lo que la ventana de cobro le mostró al
            // cajero (lo que el cliente pagó). Si se recalculara aquí, los minutos que tarda
            // el cobro podrían cruzar otra fracción y guardar/imprimir un monto mayor al cobrado.
            var baseAmount = quotedBaseAmount is { } quoted && quoted >= 0
                ? quoted
                : CalculateAmount(session, exitAt);
            var amount = baseAmount + extraAmount;

            decimal? changeAmount = null;
            if (paymentMethod == PaymentMethods.Cash && tenderedAmount is { } tendered)
            {
                if (tendered < amount)
                {
                    throw new InvalidOperationException("El efectivo recibido es menor al monto a cobrar.");
                }

                changeAmount = tendered - amount;
            }

            // Desglose por forma de pago: en pagos puros se deriva del total; en el pago mixto
            // las dos partes las digita el cajero y deben sumar exactamente el total.
            var (cashAmount, sinpeAmount) = paymentMethod switch
            {
                PaymentMethods.Cash => (amount, 0m),
                PaymentMethods.Sinpe => (0m, amount),
                _ => (cashPortion ?? 0m, sinpePortion ?? 0m)
            };
            if (paymentMethod == PaymentMethods.Mixed)
            {
                if (cashAmount < 0 || sinpeAmount < 0)
                {
                    throw new InvalidOperationException("Los montos del pago mixto no pueden ser negativos.");
                }

                if (cashAmount + sinpeAmount != amount)
                {
                    throw new InvalidOperationException("En el pago mixto, efectivo + SINPE debe sumar exactamente el total a cobrar.");
                }
            }

            using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE dbo.ParkingSessions
                    SET ExitAt = @ExitAt,
                        ExitedByUserId = @UserId,
                        ChargedAmount = @Amount,
                        ExtraAmount = @ExtraAmount,
                        ChargedDays = @ChargedDays,
                        Status = 'C'
                    WHERE SessionId = @SessionId AND Status = 'A';
                    """;
                update.Parameters.AddWithValue("@ExitAt", exitAt);
                update.Parameters.AddWithValue("@UserId", userId);
                update.Parameters.AddWithValue("@Amount", amount);
                update.Parameters.AddWithValue("@ExtraAmount", extraAmount);
                update.Parameters.AddWithValue("@ChargedDays", (object?)chargedDays ?? DBNull.Value);
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
                    INSERT INTO dbo.Payments(SessionId, Amount, PaidAt, UserId, PaymentMethod, Reference, TenderedAmount, ChangeAmount, CashAmount, SinpeAmount)
                    VALUES (@SessionId, @Amount, @PaidAt, @UserId, @PaymentMethod, @Reference, @Tendered, @Change, @CashAmount, @SinpeAmount);
                    """;
                payment.Parameters.AddWithValue("@SessionId", sessionId);
                payment.Parameters.AddWithValue("@Amount", amount);
                payment.Parameters.AddWithValue("@PaidAt", paidAt);
                payment.Parameters.AddWithValue("@UserId", userId);
                payment.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                payment.Parameters.AddWithValue("@Reference", (object?)reference ?? DBNull.Value);
                payment.Parameters.AddWithValue("@Tendered", (object?)tenderedAmount ?? DBNull.Value);
                payment.Parameters.AddWithValue("@Change", (object?)changeAmount ?? DBNull.Value);
                payment.Parameters.AddWithValue("@CashAmount", cashAmount);
                payment.Parameters.AddWithValue("@SinpeAmount", sinpeAmount);
                payment.ExecuteNonQuery();
            }

            transaction.Commit();
            AuditService.Log(userId, "RegistrarSalida", "ParkingSessions", sessionId.ToString(),
                $"Placa {session.Plate}, cobro {amount:0.00} ({paymentMethod}), extra {extraAmount:0.00}"
                + $", monto congelado {exitAt:yyyy-MM-dd HH:mm:ss}, pago {paidAt:yyyy-MM-dd HH:mm:ss}"
                + (chargedDays is { } days ? $", por dia x{days}" : string.Empty)
                + (paymentMethod == PaymentMethods.Mixed ? $", efectivo {cashAmount:0.00}, SINPE {sinpeAmount:0.00}" : string.Empty));
            return GetSessionById(sessionId) ?? throw new InvalidOperationException("No se pudo leer la salida registrada.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>Obtiene el pago registrado de una estadía (para reimprimir el comprobante).</summary>
    public Payment? GetPaymentBySession(long sessionId)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.PaymentId, p.SessionId, p.Amount, p.PaidAt, p.UserId, p.PaymentMethod,
                   p.Reference, p.TenderedAmount, p.ChangeAmount, p.CashAmount, p.SinpeAmount,
                   u.FullName AS Username
            FROM dbo.Payments p
            INNER JOIN dbo.Users u ON u.UserId = p.UserId
            WHERE p.SessionId = @SessionId;
            """;
        command.Parameters.AddWithValue("@SessionId", sessionId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Payment
        {
            PaymentId = reader.GetInt64(reader.GetOrdinal("PaymentId")),
            SessionId = reader.GetInt64(reader.GetOrdinal("SessionId")),
            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
            PaidAt = reader.GetDateTime(reader.GetOrdinal("PaidAt")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod")),
            Reference = reader.IsDBNull(reader.GetOrdinal("Reference")) ? null : reader.GetString(reader.GetOrdinal("Reference")),
            TenderedAmount = GetNullableDecimal(reader, "TenderedAmount"),
            ChangeAmount = GetNullableDecimal(reader, "ChangeAmount"),
            CashAmount = GetNullableDecimal(reader, "CashAmount"),
            SinpeAmount = GetNullableDecimal(reader, "SinpeAmount"),
            Username = reader.GetString(reader.GetOrdinal("Username"))
        };
    }

    /// <summary>Aplica una tarifa personalizada (solo para esta estadía) a una sesión activa.</summary>
    public void SetCustomRate(long sessionId, string rateType, decimal amount, int graceMinutes, int? blockMinutes, decimal? blockAmount, string? note, int userId)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("El monto de la tarifa personalizada no puede ser negativo.");
        }

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.ParkingSessions
            SET CustomRateType = @RateType,
                CustomRateAmount = @Amount,
                CustomGraceMinutes = @Grace,
                CustomBlockMinutes = @BlockMinutes,
                CustomBlockAmount = @BlockAmount,
                CustomNote = @Note
            WHERE SessionId = @SessionId AND Status = 'A';
            """;
        command.Parameters.AddWithValue("@RateType", rateType);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@Grace", graceMinutes);
        command.Parameters.AddWithValue("@BlockMinutes", (object?)blockMinutes ?? DBNull.Value);
        command.Parameters.AddWithValue("@BlockAmount", (object?)blockAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("@Note", (object?)note ?? DBNull.Value);
        command.Parameters.AddWithValue("@SessionId", sessionId);
        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("No se pudo aplicar la tarifa personalizada (¿el vehículo ya salió?).");
        }

        AuditService.Log(userId, "TarifaPersonalizada", "ParkingSessions", sessionId.ToString(),
            $"{rateType} {amount:0.00}{(note is null ? string.Empty : $" - {note}")}");
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

    /// <summary>Calcula el monto a cobrar usando la tarifa efectiva de la estadía (incluye tarifa personalizada).</summary>
    public static decimal CalculateAmount(ParkingSession session, DateTime exitAt) =>
        CalculateAmount(session.EntryAt, exitAt, session.EffectiveRateType, session.EffectiveRateAmount,
            session.EffectiveGraceMinutes, session.EffectiveBlockMinutes, session.EffectiveBlockAmount);

    /// <summary>
    /// El cobro empieza desde el ingreso (nunca sale gratis). En la tarifa normal la gracia es
    /// cero: los primeros 10 minutos después de una hora cuestan ₡200 y cada tramo posterior
    /// suma ₡100. El parámetro de gracia se conserva para tarifas personalizadas.
    /// </summary>
    public static decimal CalculateAmount(
        DateTime entryAt,
        DateTime exitAt,
        string rateType,
        decimal amount,
        int graceMinutes,
        int? blockMinutes = null,
        decimal? blockAmount = null)
    {
        // Se cobra por minutos COMPLETOS: los segundos nunca cuentan. El tiempo en pantalla
        // se muestra en minutos, así que sin esto una estadía que se ve como "1h 0m" (pero
        // lleva 1h y unos segundos) cobraba la siguiente fracción de 10 minutos.
        var minutes = Math.Floor(Math.Max(0, (exitAt - entryAt).TotalMinutes));

        if (rateType == "Hora")
        {
            return CalculateHourlyAmount(minutes, amount, graceMinutes, blockMinutes, blockAmount);
        }

        var billableMinutes = Math.Max(0, minutes - graceMinutes);
        var units = rateType switch
        {
            "Dia" => Math.Max(1, Math.Ceiling(billableMinutes / 1440d)),
            "Semana" => Math.Max(1, Math.Ceiling(billableMinutes / 10080d)),
            "Mes" => Math.Max(1, Math.Ceiling(billableMinutes / 43200d)),
            _ => 1d // Fija
        };

        return amount * Convert.ToDecimal(units);
    }

    /// <summary>
    /// Tarifa por hora: la PRIMERA hora se cobra completa desde que el vehículo ingresa
    /// (ej. ₡700 aunque lleve 5 minutos). A partir de cada hora completa, el excedente se
    /// cobra según la tabla de fracciones de 10 minutos (10m=₡200, 20m=₡300, 30m=₡400,
    /// 40m=₡500, 50m=₡600, hora completa=monto de la hora). En tarifas que tengan una gracia
    /// personalizada, la fracción no se cobra dentro de esa gracia. Con tope por bloque
    /// (ej. ₡3000 por cada 12h) el bloque nunca cobra más del tope: al superarlo, la
    /// estadía pasa automáticamente a la tarifa diaria.
    /// </summary>
    private static decimal CalculateHourlyAmount(double minutes, decimal amount, int graceMinutes, int? blockMinutes, decimal? blockAmount)
    {
        var hasCap = blockMinutes is > 0 && blockAmount is > 0;
        long fullBlocks = 0;
        var remaining = minutes;
        if (hasCap)
        {
            fullBlocks = (long)Math.Floor(minutes / blockMinutes!.Value);
            remaining = minutes - (fullBlocks * (double)blockMinutes.Value);
        }

        var fullHours = (long)Math.Floor(remaining / 60d);
        var fractionMinutes = remaining - (fullHours * 60d);

        // Al inicio de la estadía la primera hora se cobra completa por adelantado; la tabla
        // de fracciones solo aplica al excedente después de una hora completa.
        if (fullBlocks == 0 && fullHours == 0)
        {
            return hasCap ? Math.Min(amount, blockAmount!.Value) : amount;
        }

        var fractionCost = fractionMinutes > graceMinutes ? FractionPrice(fractionMinutes, amount) : 0m;
        var blockCost = (fullHours * amount) + fractionCost;
        if (!hasCap)
        {
            return blockCost;
        }

        var cap = blockAmount!.Value;
        return (fullBlocks * cap) + Math.Min(blockCost, cap);
    }

    /// <summary>Precio de la fracción final en tramos de 10 minutos, sin superar el monto de la hora.</summary>
    private static decimal FractionPrice(double fractionMinutes, decimal hourAmount)
    {
        var tier = Math.Max(1, (int)Math.Ceiling(fractionMinutes / 10d));
        return Math.Min(100m + (tier * 100m), hourAmount);
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
            r.BlockMinutes,
            r.BlockAmount,
            entered.FullName AS EnteredBy,
            exited.FullName AS ExitedBy,
            s.Status,
            s.ChargedAmount,
            s.ExtraAmount,
            s.ChargedDays,
            s.CustomRateType,
            s.CustomRateAmount,
            s.CustomGraceMinutes,
            s.CustomBlockMinutes,
            s.CustomBlockAmount,
            s.CustomNote,
            pay.PaymentMethod
        FROM dbo.ParkingSessions s
        INNER JOIN dbo.ParkingRates r ON r.RateId = s.RateId
        INNER JOIN dbo.Users entered ON entered.UserId = s.EnteredByUserId
        LEFT JOIN dbo.Users exited ON exited.UserId = s.ExitedByUserId
        LEFT JOIN dbo.Payments pay ON pay.SessionId = s.SessionId
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
        BlockMinutes = GetNullableInt(reader, "BlockMinutes"),
        BlockAmount = GetNullableDecimal(reader, "BlockAmount"),
        EnteredBy = reader.GetString(reader.GetOrdinal("EnteredBy")),
        ExitedBy = reader.IsDBNull(reader.GetOrdinal("ExitedBy")) ? null : reader.GetString(reader.GetOrdinal("ExitedBy")),
        Status = reader.GetString(reader.GetOrdinal("Status")),
        ChargedAmount = GetNullableDecimal(reader, "ChargedAmount"),
        ExtraAmount = GetNullableDecimal(reader, "ExtraAmount"),
        ChargedDays = GetNullableInt(reader, "ChargedDays"),
        CustomRateType = reader.IsDBNull(reader.GetOrdinal("CustomRateType")) ? null : reader.GetString(reader.GetOrdinal("CustomRateType")),
        CustomRateAmount = GetNullableDecimal(reader, "CustomRateAmount"),
        CustomGraceMinutes = GetNullableInt(reader, "CustomGraceMinutes"),
        CustomBlockMinutes = GetNullableInt(reader, "CustomBlockMinutes"),
        CustomBlockAmount = GetNullableDecimal(reader, "CustomBlockAmount"),
        CustomNote = reader.IsDBNull(reader.GetOrdinal("CustomNote")) ? null : reader.GetString(reader.GetOrdinal("CustomNote")),
        PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? null : reader.GetString(reader.GetOrdinal("PaymentMethod"))
    };

    private static int? GetNullableInt(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }
}
