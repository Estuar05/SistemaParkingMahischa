using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Data;

namespace SistemaParkingMahischa.Services;

/// <summary>
/// Respaldos de la base de datos. El respaldo automático corre una vez al día y
/// sobrescribe siempre el mismo archivo. La carpeta destino la configura el administrador;
/// si no hay carpeta configurada se usa la carpeta de respaldos por defecto del servidor SQL.
/// </summary>
public static class BackupService
{
    private const string BackupFileName = "ParqueoMahischa_Respaldo.bak";

    /// <summary>Carpeta donde quedarán los respaldos (configurada o la del servidor SQL).</summary>
    public static string GetTargetFolder()
    {
        var configured = LocalSettings.BackupFolder;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return GetServerDefaultBackupFolder();
    }

    /// <summary>Ruta completa del archivo de respaldo que se sobrescribe.</summary>
    public static string GetBackupFilePath() => Path.Combine(GetTargetFolder(), BackupFileName);

    /// <summary>Ejecuta el respaldo (sobrescribe el archivo fijo) y devuelve su ruta.</summary>
    public static string RunBackup()
    {
        var path = GetBackupFilePath();

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var backup = connection.CreateCommand();
        backup.CommandTimeout = 0;
        // El nombre de la BD no admite variable: se inserta con QUOTENAME (a prueba de inyección).
        // La ruta va como parámetro de sp_executesql. INIT + FORMAT sobrescriben el archivo.
        backup.CommandText = """
            DECLARE @sql nvarchar(max) =
                N'BACKUP DATABASE ' + QUOTENAME(@Db) +
                N' TO DISK = @P WITH INIT, FORMAT, SKIP, NAME = N''Respaldo Parqueo Mahischa'';';
            EXEC sp_executesql @sql, N'@P nvarchar(4000)', @P = @Path;
            """;
        backup.Parameters.AddWithValue("@Db", AppSettings.DatabaseName);
        backup.Parameters.AddWithValue("@Path", path);
        backup.ExecuteNonQuery();

        return path;
    }

    /// <summary>
    /// Respaldo automático al iniciar, una sola vez por día. Es best-effort: si algo falla,
    /// no interrumpe el arranque de la aplicación.
    /// </summary>
    public static void TryAutoBackup()
    {
        try
        {
            var marker = Path.Combine(AppContext.BaseDirectory, "ultimo_respaldo.txt");
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            if (File.Exists(marker) && File.ReadAllText(marker).Trim() == today)
            {
                return;
            }

            RunBackup();
            File.WriteAllText(marker, today);
        }
        catch
        {
            // Best-effort: el respaldo automático nunca debe impedir el ingreso.
        }
    }

    private static string GetServerDefaultBackupFolder()
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'));";
        return command.ExecuteScalar() as string ?? AppContext.BaseDirectory;
    }
}
