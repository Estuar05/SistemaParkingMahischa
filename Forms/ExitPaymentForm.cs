using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Diálogo de cobro al registrar la salida: permite agregar un monto extra (sobre-estadía),
/// elegir la forma de pago (Efectivo / SINPE) y, en efectivo, indicar con cuánto paga el
/// cliente para calcular el vuelto.
/// </summary>
public sealed class ExitPaymentForm : Form
{
    private readonly decimal _baseAmount;

    public decimal ExtraAmount { get; private set; }
    public string PaymentMethod { get; private set; } = Models.PaymentMethods.Cash;
    public string? Reference { get; private set; }
    public decimal? TenderedAmount { get; private set; }

    public ExitPaymentForm(ParkingSession session)
    {
        _baseAmount = ParkingService.CalculateAmount(session, DateTime.Now);

        Text = "Cobro de salida";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 528);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;
        Font = new Font("Segoe UI", 10F);

        var accent = new Panel { BackColor = Color.FromArgb(22, 163, 74), Dock = DockStyle.Top, Height = 6 };

        var lblPlate = new Label
        {
            Text = session.Plate,
            Font = new Font("Consolas", 26F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(22, 22),
            AutoSize = true
        };
        var lblInfo = new Label
        {
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 78),
            Size = new Size(372, 44),
            Text = $"Entrada: {session.EntryAt:dd/MM/yyyy HH:mm}\nTarifa: {(session.HasCustomRate ? "Personalizada" : session.RateName)}"
        };

        var lblBaseCaption = MakeCaption("Monto por tiempo", new Point(24, 130));
        var lblBase = new Label
        {
            Text = MoneyHelper.Format(_baseAmount),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(220, 124),
            Size = new Size(176, 28),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblExtraCaption = MakeCaption("Monto extra (sobre-estadía)", new Point(24, 166));
        var txtExtra = new TextBox
        {
            Text = "0",
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F),
            Location = new Point(220, 162),
            Width = 176
        };

        var lblTotalCaption = MakeCaption("Total a cobrar", new Point(24, 208));
        lblTotalCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblTotalCaption.ForeColor = Color.FromArgb(22, 163, 74);
        var lblTotal = new Label
        {
            Text = MoneyHelper.Format(_baseAmount),
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 163, 74),
            Location = new Point(170, 200),
            Size = new Size(226, 34),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblMethodCaption = MakeCaption("Forma de pago", new Point(24, 252));
        var rbCash = new RadioButton { Text = "Efectivo", Checked = true, Location = new Point(24, 276), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        var rbSinpe = new RadioButton { Text = "SINPE", Location = new Point(160, 276), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

        // Sección de efectivo (paga con / vuelto).
        var lblTenderCaption = MakeCaption("Paga con", new Point(24, 318));
        var txtTender = new TextBox
        {
            Text = string.Empty,
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F),
            Location = new Point(220, 314),
            Width = 176
        };
        var lblChangeCaption = MakeCaption("Vuelto", new Point(24, 358));
        var lblChange = new Label
        {
            Text = "₡0",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(202, 138, 4),
            Location = new Point(170, 352),
            Size = new Size(226, 30),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Sección de SINPE (referencia).
        var lblRefCaption = MakeCaption("Referencia / comprobante (opcional)", new Point(24, 318));
        var txtReference = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11F),
            Location = new Point(24, 342),
            Width = 372,
            Visible = false
        };
        lblRefCaption.Visible = false;

        var btnConfirm = MakeButton("Cobrar y registrar salida", new Point(24, 408), primary: true);
        btnConfirm.Size = new Size(372, 48);
        btnConfirm.BackColor = Color.FromArgb(22, 163, 74);
        var btnCancel = MakeButton("Cancelar", new Point(24, 468), primary: false);
        btnCancel.Size = new Size(372, 40);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        decimal Extra() => TryParse(txtExtra.Text, out var v) ? v : 0m;
        decimal Total() => _baseAmount + Extra();

        void RefreshTotals()
        {
            var total = Total();
            lblTotal.Text = MoneyHelper.Format(total);
            if (rbCash.Checked && TryParse(txtTender.Text, out var tendered) && txtTender.Text.Trim().Length > 0)
            {
                var change = tendered - total;
                lblChange.Text = MoneyHelper.Format(change);
                lblChange.ForeColor = change < 0 ? Color.FromArgb(220, 38, 38) : Color.FromArgb(202, 138, 4);
            }
            else
            {
                lblChange.Text = "₡0";
                lblChange.ForeColor = Color.FromArgb(202, 138, 4);
            }
        }

        void ApplyMethod()
        {
            var cash = rbCash.Checked;
            lblTenderCaption.Visible = cash;
            txtTender.Visible = cash;
            lblChangeCaption.Visible = cash;
            lblChange.Visible = cash;
            lblRefCaption.Visible = !cash;
            txtReference.Visible = !cash;
            RefreshTotals();
        }

        txtExtra.TextChanged += (_, _) => RefreshTotals();
        txtTender.TextChanged += (_, _) => RefreshTotals();
        rbCash.CheckedChanged += (_, _) => ApplyMethod();
        rbSinpe.CheckedChanged += (_, _) => ApplyMethod();

        btnConfirm.Click += (_, _) =>
        {
            if (!TryParse(txtExtra.Text, out var extra) || extra < 0)
            {
                MessageBox.Show("Digite un monto extra válido (0 o mayor).", "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var total = _baseAmount + extra;
            if (rbCash.Checked && txtTender.Text.Trim().Length > 0)
            {
                if (!TryParse(txtTender.Text, out var tendered) || tendered < 0)
                {
                    MessageBox.Show("Digite un monto válido en 'Paga con'.", "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (tendered < total)
                {
                    MessageBox.Show("El efectivo recibido es menor al total a cobrar.", "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                TenderedAmount = tendered;
            }

            ExtraAmount = extra;
            PaymentMethod = rbCash.Checked ? Models.PaymentMethods.Cash : Models.PaymentMethods.Sinpe;
            Reference = rbSinpe.Checked && txtReference.Text.Trim().Length > 0 ? txtReference.Text.Trim() : null;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(
        [
            accent, lblPlate, lblInfo,
            lblBaseCaption, lblBase,
            lblExtraCaption, txtExtra,
            lblTotalCaption, lblTotal,
            lblMethodCaption, rbCash, rbSinpe,
            lblTenderCaption, txtTender, lblChangeCaption, lblChange,
            lblRefCaption, txtReference,
            btnConfirm, btnCancel
        ]);

        AcceptButton = btnConfirm;
        ApplyMethod();
    }

    private static Label MakeCaption(string text, Point location) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(71, 85, 105),
        Location = location,
        AutoSize = true
    };

    private static bool TryParse(string value, out decimal amount) =>
        decimal.TryParse(value?.Trim().Replace("₡", string.Empty).Replace(",", string.Empty), out amount);

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
}
