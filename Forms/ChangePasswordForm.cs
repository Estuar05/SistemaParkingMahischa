using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Obliga a un usuario a definir una nueva contraseña (por ejemplo, el administrador
/// por defecto en la primera instalación). No se puede omitir.
/// </summary>
public sealed class ChangePasswordForm : Form
{
    private readonly AuthService _authService = new();
    private readonly User _user;
    private readonly TextBox _txtNew = new();
    private readonly TextBox _txtConfirm = new();
    private readonly Label _lblError = new();
    private readonly Button _btnSave = new();

    public ChangePasswordForm(User user)
    {
        _user = user;

        Text = "Cambio de contraseña obligatorio";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ClientSize = new Size(420, 320);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;

        var lblTitle = new Label
        {
            Text = "Defina su nueva contraseña",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(28, 26),
            AutoSize = true
        };
        var lblInfo = new Label
        {
            Text = $"Por seguridad, {_user.FullName} debe cambiar la contraseña antes de continuar.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(30, 62),
            Size = new Size(360, 36)
        };

        var lblNew = MakeCaption("Nueva contraseña", 110);
        StyleInput(_txtNew, 134);
        _txtNew.PasswordChar = '•';

        var lblConfirm = MakeCaption("Confirmar contraseña", 178);
        StyleInput(_txtConfirm, 202);
        _txtConfirm.PasswordChar = '•';

        _lblError.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _lblError.ForeColor = Color.FromArgb(220, 38, 38);
        _lblError.Location = new Point(30, 244);
        _lblError.Size = new Size(360, 20);

        _btnSave.Text = "Guardar y continuar";
        _btnSave.BackColor = Color.FromArgb(0, 128, 117);
        _btnSave.ForeColor = Color.White;
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _btnSave.Size = new Size(360, 44);
        _btnSave.Location = new Point(30, 268);
        _btnSave.Cursor = Cursors.Hand;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += Save;
        UiKit.RoundCorners(_btnSave, 10);
        UiKit.AttachHover(_btnSave, Color.FromArgb(0, 128, 117), Color.FromArgb(0, 96, 88));

        Controls.AddRange([lblTitle, lblInfo, lblNew, _txtNew, lblConfirm, _txtConfirm, _lblError, _btnSave]);
        AcceptButton = _btnSave;
    }

    private static Label MakeCaption(string text, int y) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(71, 85, 105),
        Location = new Point(30, y),
        AutoSize = true
    };

    private void StyleInput(TextBox textBox, int y)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 12F);
        textBox.Location = new Point(30, y);
        textBox.Size = new Size(360, 29);
        textBox.MaxLength = 80;
    }

    private void Save(object? sender, EventArgs e)
    {
        _lblError.Text = string.Empty;
        if (_txtNew.Text != _txtConfirm.Text)
        {
            _lblError.Text = "Las contraseñas no coinciden.";
            return;
        }

        if (string.Equals(_txtNew.Text, AppSettings.DefaultAdminPassword, StringComparison.Ordinal))
        {
            _lblError.Text = "No puede reutilizar la contraseña por defecto.";
            return;
        }

        try
        {
            _authService.ChangePassword(_user.UserId, _txtNew.Text);
            _user.MustChangePassword = false;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
    }
}
