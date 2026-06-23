using SistemaParkingMahischa.Controllers;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Modal que muestra la información de un vehículo (al escanear su QR o buscarlo) con
/// botones para registrar la salida y reimprimir el tiquete.
/// </summary>
public sealed class VehicleInfoForm : Form
{
    private readonly ParkingController _controller;
    private readonly User _currentUser;
    private ParkingSession _session;

    /// <summary>Indica si se registró la salida (para que la pantalla anterior se refresque).</summary>
    public bool ChangesMade { get; private set; }

    public VehicleInfoForm(ParkingSession session, ParkingController controller, User currentUser)
    {
        _session = session;
        _controller = controller;
        _currentUser = currentUser;

        Text = "Información del vehículo";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 482);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;

        var active = _session.Status == "A";
        var amount = active
            ? ParkingService.CalculateAmount(_session.EntryAt, DateTime.Now, _session.RateType, _session.RateAmount, _session.GraceMinutes)
            : _session.ChargedAmount ?? 0;

        var accent = new Panel { BackColor = active ? Color.FromArgb(22, 163, 74) : Color.FromArgb(100, 116, 139), Dock = DockStyle.Top, Height = 6 };

        var lblTitle = new Label
        {
            Text = active ? "Vehículo dentro del parqueo" : "Vehículo con salida registrada",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = active ? Color.FromArgb(22, 163, 74) : Color.FromArgb(100, 116, 139),
            Location = new Point(28, 24),
            AutoSize = true
        };
        var lblPlate = new Label
        {
            Text = _session.Plate,
            Font = new Font("Consolas", 30F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(24, 48),
            AutoSize = true
        };

        var info = new Label
        {
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.FromArgb(30, 41, 59),
            Location = new Point(28, 120),
            Size = new Size(384, 150),
            Text =
                $"Entrada:   {_session.EntryAt:dd/MM/yyyy HH:mm}\n" +
                $"Tarifa:    {_session.RateName}\n" +
                $"Tiempo:    {FormatDuration(_session.CurrentDuration)}\n" +
                (active ? "Estado:    Activo" : $"Salida:    {_session.ExitAt:dd/MM/yyyy HH:mm}")
        };

        var lblAmountCaption = new Label
        {
            Text = active ? "Monto a cobrar" : "Monto cobrado",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(28, 270),
            AutoSize = true
        };
        var lblAmount = new Label
        {
            Text = MoneyHelper.Format(amount),
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 163, 74),
            Location = new Point(26, 290),
            AutoSize = true
        };

        var btnExit = MakeButton(active ? "Registrar salida" : "Salida ya registrada", new Point(28, 356), primary: true);
        btnExit.Size = new Size(384, 48);
        btnExit.BackColor = Color.FromArgb(22, 163, 74);
        btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 135, 62);
        btnExit.Enabled = active;
        btnExit.Click += RegisterExit;

        var btnReprint = MakeButton("Reimprimir tiquete", new Point(28, 416), primary: false);
        btnReprint.Size = new Size(236, 44);
        btnReprint.Click += (_, _) =>
        {
            AuditService.Log(_currentUser.UserId, "ReimprimirTiquete", "ParkingSessions", _session.SessionId.ToString(), $"Placa {_session.Plate}");
            using var preview = new TicketPreviewForm(_session);
            preview.ShowDialog(this);
        };

        var btnClose = MakeButton("Cerrar", new Point(276, 416), primary: false);
        btnClose.Size = new Size(136, 44);
        btnClose.Click += (_, _) => Close();

        Controls.AddRange([info, lblAmount, lblAmountCaption, lblPlate, lblTitle, btnExit, btnReprint, btnClose, accent]);
    }

    private void RegisterExit(object? sender, EventArgs e)
    {
        try
        {
            var amount = ParkingService.CalculateAmount(_session.EntryAt, DateTime.Now, _session.RateType, _session.RateAmount, _session.GraceMinutes);
            var confirm = MessageBox.Show(
                $"¿Registrar la salida y cobrar?\n\nPlaca: {_session.Plate}\nMonto a cobrar: {MoneyHelper.Format(amount)}",
                "Confirmar salida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var closed = _controller.RegisterExit(_session.SessionId, _currentUser.UserId);
            _session = closed;
            ChangesMade = true;
            MessageBox.Show(
                $"Salida registrada.\n\nPlaca: {closed.Plate}\nMonto cobrado: {MoneyHelper.Format(closed.ChargedAmount ?? 0)}",
                "Cobro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Salida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Button MakeButton(string text, Point location, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(178, 46),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        if (primary)
        {
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
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

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        }

        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }
}
