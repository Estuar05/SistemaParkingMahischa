using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SistemaParkingMahischa.Config;

namespace SistemaParkingMahischa.Services;

public sealed record UpdateInfo(Version Version, string Tag, string DownloadUrl, string Notes);

/// <summary>
/// Actualización automática desde los "releases" de GitHub. Compara la versión instalada
/// con el último release; si hay uno más nuevo descarga el .zip y lo aplica con un script
/// que reemplaza los archivos cuando la aplicación se cierra, y la vuelve a abrir.
/// </summary>
public static class UpdateService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    /// <summary>Consulta GitHub. Devuelve la info si hay una versión más nueva; null si no hay o falla.</summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        var repo = AppSettings.UpdateRepository.Trim();
        if (string.IsNullOrWhiteSpace(repo))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/releases/latest");
            request.Headers.UserAgent.ParseAdd("ParqueoMahischa-Updater");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await Http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;

            if (root.TryGetProperty("draft", out var draft) && draft.GetBoolean())
            {
                return null;
            }

            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var version = ParseVersion(tag);
            if (version is null || version <= CurrentVersion)
            {
                return null;
            }

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? string.Empty;
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                return null;
            }

            var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty;
            return new UpdateInfo(version, tag, downloadUrl, notes);
        }
        catch
        {
            return null; // Sin internet o repo inaccesible: simplemente no se ofrece la actualización.
        }
    }

    /// <summary>
    /// Descarga el paquete, prepara el script de actualización, lo lanza y cierra la aplicación.
    /// El script espera a que la app cierre, reemplaza los archivos y la reinicia.
    /// </summary>
    public static async Task DownloadAndApplyAsync(UpdateInfo info)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ParqueoMahischaUpdate");
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }

        Directory.CreateDirectory(tempRoot);

        var zipPath = Path.Combine(tempRoot, "update.zip");
        var extractDir = Path.Combine(tempRoot, "extracted");

        using (var response = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(zipPath);
            await response.Content.CopyToAsync(fileStream);
        }

        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var sourceDir = ResolveSourceRoot(extractDir);
        var appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var exePath = Environment.ProcessPath ?? Path.Combine(appDir, "SistemaParkingMaisha.exe");

        var batchPath = Path.Combine(Path.GetTempPath(), "ParqueoMahischa_update.bat");
        await File.WriteAllTextAsync(batchPath, BuildUpdaterScript(Environment.ProcessId, sourceDir, appDir, exePath, tempRoot), new UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchPath}\"",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        Application.Exit();
    }

    private static string ResolveSourceRoot(string extractDir)
    {
        // Si el zip trae una sola carpeta raíz, los archivos están dentro de ella.
        var entries = Directory.GetFileSystemEntries(extractDir);
        if (entries.Length == 1 && Directory.Exists(entries[0]))
        {
            return entries[0];
        }

        return extractDir;
    }

    private static string BuildUpdaterScript(int pid, string sourceDir, string appDir, string exePath, string tempRoot)
    {
        return $"""
            @echo off
            chcp 65001 >nul
            title Actualizando Parqueo Mahischa
            :wait
            tasklist /FI "PID eq {pid}" 2>nul | find "{pid}" >nul
            if "%errorlevel%"=="0" (
                timeout /t 1 /nobreak >nul
                goto wait
            )
            robocopy "{sourceDir}" "{appDir}" /E /R:3 /W:2 /XF "SistemaParkingMaisha.dll.config" "ultimo_respaldo.txt" /NFL /NDL /NJH /NJS /NC /NS /NP >nul
            start "" "{exePath}"
            rmdir /s /q "{tempRoot}" >nul 2>&1
            (goto) 2>nul & del "%~f0"
            """;
    }

    private static Version? ParseVersion(string tag)
    {
        var cleaned = new string(tag.SkipWhile(c => !char.IsDigit(c)).ToArray());
        return Version.TryParse(cleaned, out var version) ? version : null;
    }
}
