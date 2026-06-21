using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Data;

public static class DatabaseInitializer
{
    public static void EnsureCreated()
    {
        EnsureDatabaseExists();

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        var batchNumber = 0;
        foreach (var batch in SplitSqlBatches(DatabaseSchema.Script))
        {
            batchNumber++;
            if (!string.IsNullOrWhiteSpace(batch))
            {
                try
                {
                    SqlDatabase.ExecuteBatch(connection, batch);
                }
                catch (SqlException ex)
                {
                    var firstLine = batch.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    throw new InvalidOperationException($"Error creando la base de datos en el lote {batchNumber}: {firstLine}. Detalle SQL: {ex.Message}", ex);
                }
            }
        }

        new AuthService().EnsureDefaultAdmin();
    }

    private static void EnsureDatabaseExists()
    {
        using var connection = SqlDatabase.CreateMasterConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandTimeout = 0;
        command.CommandText = $"""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
                EXEC(@sql);
            END
            """;
        command.Parameters.AddWithValue("@DatabaseName", AppSettings.DatabaseName);
        command.ExecuteNonQuery();
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        using var reader = new StringReader(script);
        var batch = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return string.Join(Environment.NewLine, batch);
                batch.Clear();
                continue;
            }

            batch.Add(line);
        }

        if (batch.Count > 0)
        {
            yield return string.Join(Environment.NewLine, batch);
        }
    }
}
