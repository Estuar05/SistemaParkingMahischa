using System.Text.Json;

namespace SistemaParkingMahischa.Config;

/// <summary>
/// Configuración local persistente (compartida por la máquina) que la aplicación puede
/// escribir en tiempo de ejecución, a diferencia de App.config. Se guarda en
/// %ProgramData%\Parqueo Mahischa\settings.json.
/// </summary>
public static class LocalSettings
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Parqueo Mahischa");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    private static SettingsData _data = Load();

    /// <summary>Carpeta destino de los respaldos. Si es null se usa la carpeta por defecto del servidor SQL.</summary>
    public static string? BackupFolder
    {
        get => _data.BackupFolder;
        set
        {
            _data.BackupFolder = value;
            Save();
        }
    }

    private static SettingsData Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData();
            }
        }
        catch
        {
            // Configuración corrupta o inaccesible: se usa la de por defecto.
        }

        return new SettingsData();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Si no se puede persistir, la configuración queda solo en memoria para esta sesión.
        }
    }

    private sealed class SettingsData
    {
        public string? BackupFolder { get; set; }
    }
}
