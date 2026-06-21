namespace SistemaParkingMahischa.Forms;

/// <summary>Pequeño diálogo modal para confirmar una acción pidiendo la contraseña.</summary>
public sealed class PasswordPromptForm : Form
{
    private readonly TextBox _txtPassword = new();

    public string Password => _txtPassword.Text;

    public PasswordPromptForm(string message)
    {
        Text = "Confirmar contraseña";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 170);
        BackColor = Color.White;

        var lblMessage = new Label
        {
            Text = message,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(24, 22),
            Size = new Size(332, 40)
        };

        _txtPassword.BorderStyle = BorderStyle.FixedSingle;
        _txtPassword.Font = new Font("Segoe UI", 12F);
        _txtPassword.PasswordChar = '•';
        _txtPassword.Location = new Point(24, 70);
        _txtPassword.Size = new Size(332, 30);

        var btnOk = new Button
        {
            Text = "Aceptar",
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(0, 128, 117),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(160, 40),
            Location = new Point(24, 116)
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(160, 40),
            Location = new Point(196, 116)
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);

        Controls.AddRange([lblMessage, _txtPassword, btnOk, btnCancel]);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
