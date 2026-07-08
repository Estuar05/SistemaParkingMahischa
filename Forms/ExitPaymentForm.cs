using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Diálogo de cobro al registrar la salida: permite agregar un monto extra (sobre-estadía)
/// y elegir la forma de pago (Efectivo / SINPE / Mixto). En efectivo calcula el vuelto;
/// en mixto pide cuánto se paga con cada forma (deben sumar el total).
/// </summary>
public sealed class ExitPaymentForm : Form
{
    private readonly decimal _baseAmount;

    /// <summary>
    /// Monto por tiempo mostrado al cajero (calculado al abrir la ventana). La salida se
    /// registra con ESTE monto: es lo que el cliente ya pagó, aunque el cobro tarde unos
    /// minutos y la estadía cruce otra fracción mientras la ventana está abierta.
    /// </summary>
    public decimal QuotedBaseAmount => _baseAmount;

    public decimal ExtraAmount { get; private set; }
    public string PaymentMethod { get; private set; } = Models.PaymentMethods.Cash;
    public string? Reference { get; private set; }
    public decimal? TenderedAmount { get; private set; }
    public decimal? CashPortion { get; private set; }
    public decimal? SinpePortion { get; private set; }

    public ExitPaymentForm(ParkingSession session)
    {
        _baseAmount = ParkingService.CalculateAmount(session, DateTime.Now);

        Text = "Cobro de salida";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 590);
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
        var rbSinpe = new RadioButton { Text = "SINPE", Location = new Point(146, 276), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        var rbMixed = new RadioButton { Text = "Mixto", Location = new Point(252, 276), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };

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

        // Sección de pago mixto (efectivo + SINPE): al digitar el efectivo, el SINPE se
        // completa solo con lo que falta (y viceversa) para que siempre sumen el total.
        var lblMixedCashCaption = MakeCaption("Pagado en efectivo", new Point(24, 318));
        var txtMixedCash = new TextBox
        {
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F),
            Location = new Point(220, 314),
            Width = 176,
            Visible = false
        };
        var lblMixedSinpeCaption = MakeCaption("Pagado por SINPE", new Point(24, 358));
        var txtMixedSinpe = new TextBox
        {
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F),
            Location = new Point(220, 354),
            Width = 176,
            Visible = false
        };
        lblMixedCashCaption.Visible = false;
        lblMixedSinpeCaption.Visible = false;

        // Sección de SINPE (referencia): también visible en el pago mixto.
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

        var btnConfirm = MakeButton("Cobrar y registrar salida", new Point(24, 470), primary: true);
        btnConfirm.Size = new Size(372, 48);
        btnConfirm.BackColor = Color.FromArgb(22, 163, 74);
        var btnCancel = MakeButton("Cancelar", new Point(24, 528), primary: false);
        btnCancel.Size = new Size(372, 40);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        decimal Extra() => TryParse(txtExtra.Text, out var v) ? v : 0m;
        decimal Total() => _baseAmount + Extra();

        var syncingMixed = false;

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

        void SyncMixed(TextBox edited, TextBox other)
        {
            if (syncingMixed)
            {
                return;
            }

            syncingMixed = true;
            try
            {
                var value = TryParse(edited.Text, out var v) && edited.Text.Trim().Length > 0 ? v : 0m;
                var remaining = Total() - value;
                other.Text = remaining > 0 ? remaining.ToString("0") : "0";
            }
            finally
            {
                syncingMixed = false;
            }
        }

        void ApplyMethod()
        {
            var cash = rbCash.Checked;
            var sinpe = rbSinpe.Checked;
            var mixed = rbMixed.Checked;
            lblTenderCaption.Visible = cash;
            txtTender.Visible = cash;
            lblChangeCaption.Visible = cash;
            lblChange.Visible = cash;
            lblMixedCashCaption.Visible = mixed;
            txtMixedCash.Visible = mixed;
            lblMixedSinpeCaption.Visible = mixed;
            txtMixedSinpe.Visible = mixed;
            lblRefCaption.Visible = sinpe || mixed;
            txtReference.Visible = sinpe || mixed;
            if (sinpe)
            {
                lblRefCaption.Location = new Point(24, 318);
                txtReference.Location = new Point(24, 342);
            }
            else if (mixed)
            {
                lblRefCaption.Location = new Point(24, 398);
                txtReference.Location = new Point(24, 422);
            }

            RefreshTotals();
        }

        txtExtra.TextChanged += (_, _) =>
        {
            RefreshTotals();
            if (rbMixed.Checked)
            {
                SyncMixed(txtMixedCash, txtMixedSinpe);
            }
        };
        txtTender.TextChanged += (_, _) => RefreshTotals();
        txtMixedCash.TextChanged += (_, _) => SyncMixed(txtMixedCash, txtMixedSinpe);
        txtMixedSinpe.TextChanged += (_, _) => SyncMixed(txtMixedSinpe, txtMixedCash);
        rbCash.CheckedChanged += (_, _) => ApplyMethod();
        rbSinpe.CheckedChanged += (_, _) => ApplyMethod();
        rbMixed.CheckedChanged += (_, _) => ApplyMethod();

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

            if (rbMixed.Checked)
            {
                if (!TryParse(txtMixedCash.Text, out var cashPart) || cashPart < 0
                    || !TryParse(txtMixedSinpe.Text, out var sinpePart) || sinpePart < 0)
                {
                    MessageBox.Show("Digite montos válidos de efectivo y SINPE.", "Cobro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cashPart + sinpePart != total)
                {
                    MessageBox.Show(
                        $"Efectivo + SINPE debe sumar exactamente el total a cobrar ({MoneyHelper.Format(total)}).",
                        "Cobro",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                CashPortion = cashPart;
                SinpePortion = sinpePart;
            }

            ExtraAmount = extra;
            PaymentMethod = rbCash.Checked
                ? Models.PaymentMethods.Cash
                : rbSinpe.Checked ? Models.PaymentMethods.Sinpe : Models.PaymentMethods.Mixed;
            Reference = !rbCash.Checked && txtReference.Text.Trim().Length > 0 ? txtReference.Text.Trim() : null;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(
        [
            accent, lblPlate, lblInfo,
            lblBaseCaption, lblBase,
            lblExtraCaption, txtExtra,
            lblTotalCaption, lblTotal,
            lblMethodCaption, rbCash, rbSinpe, rbMixed,
            lblTenderCaption, txtTender, lblChangeCaption, lblChange,
            lblMixedCashCaption, txtMixedCash, lblMixedSinpeCaption, txtMixedSinpe,
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
