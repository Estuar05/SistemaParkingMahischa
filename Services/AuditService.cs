using SistemaParkingMahischa.Data;

namespace SistemaParkingMahischa.Services;

/// <summary>
/// Registra acciones sensibles en la tabla dbo.AuditLogs. Es "best-effort":
/// si falla el registro, nunca interrumpe la operación principal del usuario.
/// </summary>
public static class AuditService
{
    public static void Log(int? userId, string actionKey, string entityName, string? entityId, string? details)
    {
        try
        {
            using var connection = SqlDatabase.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.AuditLogs(UserId, ActionKey, EntityName, EntityId, Details)
                VALUES (@UserId, @ActionKey, @EntityName, @EntityId, @Details);
                """;
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ActionKey", Trim(actionKey, 80));
            command.Parameters.AddWithValue("@EntityName", Trim(entityName, 80));
            command.Parameters.AddWithValue("@EntityId", (object?)Trim(entityId, 80) ?? DBNull.Value);
            command.Parameters.AddWithValue("@Details", (object?)Trim(details, 500) ?? DBNull.Value);
            command.ExecuteNonQuery();
        }
        catch
        {
            // La auditoría nunca debe interrumpir la operación del usuario.
        }
    }

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
