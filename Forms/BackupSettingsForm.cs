using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Configuración de respaldos (solo administrador). Muestra dónde se guardan, permite
/// cambiar la ruta (pidiendo la contraseña) y ejecutar un respaldo manual.
/// </summary>
public sealed class BackupSettingsForm : Form
{
    private readonly AuthService _authService = new();
    private readonly User _currentUser;
    private readonly Label _lblPath = new();

    public BackupSettingsForm(User currentUser)
    {
        _currentUser = currentUser;

        Text = "Configuración de respaldos";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 320);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;

        var lblTitle = new Label
        {
            Text = "Respaldos de la base de datos",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(24, 22),
            AutoSize = true
        };
        var lblInfo = new Label
        {
            Text = "El sistema crea un respaldo automático todos los días (sobrescribe el mismo archivo). "
                 + "Aquí puede elegir la carpeta destino o respaldar manualmente.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(26, 58),
            Size = new Size(510, 44)
        };

        var lblPathCaption = new Label
        {
            Text = "Carpeta actual",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(26, 116),
            AutoSize = true
        };
        _lblPath.Font = new Font("Segoe UI", 10F);
        _lblPath.ForeColor = Color.FromArgb(30, 41, 59);
        _lblPath.Location = new Point(26, 138);
        _lblPath.Size = new Size(510, 44);
        RefreshPath();

        var btnChange = MakeButton("Cambiar ruta...", new Point(26, 196), primary: false);
        btnChange.Click += ChangePath;

        var btnDefault = MakeButton("Usar carpeta del servidor", new Point(206, 196), primary: false);
        btnDefault.Width = 220;
        btnDefault.Click += UseServerDefault;

        var btnBackupNow = MakeButton("Respaldar ahora", new Point(26, 250), primary: true);
        btnBackupNow.Width = 200;
        btnBackupNow.Click += BackupNow;

        var btnClose = MakeButton("Cerrar", new Point(420, 250), primary: false);
        btnClose.Width = 116;
        btnClose.Click += (_, _) => Close();

        Controls.AddRange([lblTitle, lblInfo, lblPathCaption, _lblPath, btnChange, btnDefault, btnBackupNow, btnClose]);
    }

    private void RefreshPath()
    {
        try
        {
            var configured = LocalSettings.BackupFolder;
            _lblPath.Text = string.IsNullOrWhiteSpace(configured)
                ? $"Carpeta por defecto del servidor SQL:\n{BackupService.GetTargetFolder()}"
                : configured;
        }
        catch
        {
            _lblPath.Text = LocalSettings.BackupFolder ?? "(no se pudo determinar la carpeta del servidor)";
        }
    }

    private void ChangePath(object? sender, EventArgs e)
    {
        using var prompt = new PasswordPromptForm("Para cambiar la ruta de respaldos, ingrese su contraseña de administrador.");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!_authService.VerifyUserPassword(_currentUser.UserId, prompt.Password))
        {
            MessageBox.Show("Contraseña incorrecta.", "Respaldos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleccione la carpeta donde se guardarán los respaldos",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        LocalSettings.BackupFolder = dialog.SelectedPath;
        AuditService.Log(_currentUser.UserId, "CambioRutaRespaldo", "Backup", null, dialog.SelectedPath);
        RefreshPath();

        var test = MessageBox.Show(
            "Ruta guardada. ¿Desea probar un respaldo ahora para confirmar que el servidor SQL puede escribir en esa carpeta?",
            "Respaldos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (test == DialogResult.Yes)
        {
            BackupNow(sender, e);
        }
    }

    private void UseServerDefault(object? sender, EventArgs e)
    {
        using var prompt = new PasswordPromptForm("Para restablecer la carpeta de respaldos, ingrese su contraseña de administrador.");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!_authService.VerifyUserPassword(_currentUser.UserId, prompt.Password))
        {
            MessageBox.Show("Contraseña incorrecta.", "Respaldos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        LocalSettings.BackupFolder = null;
        AuditService.Log(_currentUser.UserId, "CambioRutaRespaldo", "Backup", null, "Carpeta por defecto del servidor");
        RefreshPath();
    }

    private void BackupNow(object? sender, EventArgs e)
    {
        try
        {
            UseWaitCursor = true;
            var path = BackupService.RunBackup();
            AuditService.Log(_currentUser.UserId, "RespaldoManual", "Backup", null, path);
            MessageBox.Show($"Respaldo creado correctamente en:\n\n{path}", "Respaldos", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo crear el respaldo en esa carpeta. Verifique que el servicio de SQL Server "
                + $"tenga permiso de escritura allí.\n\nDetalle: {ex.Message}",
                "Respaldos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static Button MakeButton(string text, Point location, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(170, 42),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        if (primary)
        {
            button.BackColor = Color.FromArgb(0, 128, 117);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 96, 88);
        }
        else
        {
            button.BackColor = Color.FromArgb(246, 249, 251);
            button.ForeColor = Color.FromArgb(40, 52, 65);
            button.FlatAppearance.BorderColor = Color.FromArgb(202, 213, 224);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(233, 239, 244);
        }

        UiKit.RoundCorners(button, 8);
        return button;
    }
}
