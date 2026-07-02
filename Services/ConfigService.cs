using System.Globalization;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Data;

namespace SistemaParkingMahischa.Services;

/// <summary>
/// Configuración guardada en la base de datos (tabla dbo.AppConfig), para que valores como el
/// fondo de caja sean los mismos en todas las instalaciones sin depender del archivo .config
/// local, que el actualizador nunca reemplaza.
/// </summary>
public static class ConfigService
{
    /// <summary>Fondo de caja (base) que siempre debe quedar en la caja física.</summary>
    public static decimal GetMinimumCashAmount()
    {
        var stored = GetValue("MinimumCashAmount");
        return decimal.TryParse(stored, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0
            ? value
            : AppSettings.MinimumCashAmount;
    }

    public static void SetMinimumCashAmount(decimal amount, int userId)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("El fondo de caja no puede ser negativo.");
        }

        SetValue("MinimumCashAmount", amount.ToString(CultureInfo.InvariantCulture));
        AuditService.Log(userId, "ConfigurarFondoCaja", "AppConfig", "MinimumCashAmount", $"Nuevo fondo {amount:0.00}");
    }

    private static string? GetValue(string key)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ConfigValue FROM dbo.AppConfig WHERE ConfigKey = @Key;";
        command.Parameters.AddWithValue("@Key", key);
        return command.ExecuteScalar() as string;
    }

    private static void SetValue(string key, string value)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.AppConfig AS target
            USING (SELECT @Key AS ConfigKey) AS source ON target.ConfigKey = source.ConfigKey
            WHEN MATCHED THEN UPDATE SET ConfigValue = @Value, UpdatedAt = SYSDATETIME()
            WHEN NOT MATCHED THEN INSERT (ConfigKey, ConfigValue) VALUES (@Key, @Value);
            """;
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        command.ExecuteNonQuery();
    }
}
