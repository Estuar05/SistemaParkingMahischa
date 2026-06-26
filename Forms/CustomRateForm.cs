using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Configura una tarifa personalizada para una sola estadía: se elige la unidad de tiempo
/// (hora, día, semana, mes o monto fijo) y el monto que se cobrará por cada unidad.
/// </summary>
public sealed class CustomRateForm : Form
{
    public string RateType { get; private set; } = "Hora";
    public decimal Amount { get; private set; }
    public int GraceMinutes { get; private set; }
    public int? BlockMinutes { get; private set; }
    public decimal? BlockAmount { get; private set; }
    public string? Note { get; private set; }

    public CustomRateForm(ParkingSession session)
    {
        Text = "Tarifa personalizada";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 470);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;
        Font = new Font("Segoe UI", 10F);

        var accent = new Panel { BackColor = Color.FromArgb(124, 58, 237), Dock = DockStyle.Top, Height = 6 };
        var lblTitle = new Label
        {
            Text = $"Tarifa especial · {session.Plate}",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(22, 22),
            AutoSize = true
        };
        var lblHint = new Label
        {
            Text = "Aplica solo a esta estadía. El cobro se calcula con el tiempo dentro del parqueo.",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(24, 56),
            Size = new Size(372, 36)
        };

        var cmbType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11F),
            Location = new Point(24, 122),
            Width = 372
        };
        cmbType.Items.AddRange(["Hora", "Dia", "Semana", "Mes", "Fija"]);
        cmbType.SelectedIndex = 0;

        var txtAmount = new TextBox
        {
            Text = "0",
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12F),
            Location = new Point(24, 184),
            Width = 372
        };

        var numGrace = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11F),
            Location = new Point(24, 246),
            Width = 372,
            Maximum = 1440
        };

        var chkBlock = new CheckBox
        {
            Text = "Tope por bloque (cada 12h no cobrar más de…)",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 296),
            AutoSize = true
        };
        var txtBlockAmount = new TextBox
        {
            Text = "3000",
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11F),
            Location = new Point(24, 322),
            Width = 372,
            Enabled = false
        };

        var txtNote = new TextBox
        {
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            Location = new Point(24, 372),
            Width = 372,
            PlaceholderText = "Motivo / nota (opcional)"
        };

        var btnSave = MakeButton("Guardar tarifa", new Point(24, 410), true);
        btnSave.Size = new Size(236, 44);
        var btnCancel = MakeButton("Cancelar", new Point(272, 410), false);
        btnCancel.Size = new Size(124, 44);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        void ApplyType()
        {
            var isHour = (cmbType.SelectedItem?.ToString() ?? "Hora") == "Hora";
            chkBlock.Visible = isHour;
            txtBlockAmount.Enabled = isHour && chkBlock.Checked;
            txtBlockAmount.Visible = isHour;
        }

        cmbType.SelectedIndexChanged += (_, _) => ApplyType();
        chkBlock.CheckedChanged += (_, _) => txtBlockAmount.Enabled = chkBlock.Checked;

        btnSave.Click += (_, _) =>
        {
            if (!decimal.TryParse(txtAmount.Text.Trim(), out var amount) || amount < 0)
            {
                MessageBox.Show("Digite un monto válido.", "Tarifa personalizada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RateType = cmbType.SelectedItem?.ToString() ?? "Hora";
            Amount = amount;
            GraceMinutes = Convert.ToInt32(numGrace.Value);
            if (RateType == "Hora" && chkBlock.Checked && decimal.TryParse(txtBlockAmount.Text.Trim(), out var block) && block > 0)
            {
                BlockMinutes = 720;
                BlockAmount = block;
            }
            else
            {
                BlockMinutes = null;
                BlockAmount = null;
            }

            Note = txtNote.Text.Trim().Length > 0 ? txtNote.Text.Trim() : null;
            DialogResult = DialogResult.OK;
            Close();
        };

        // Carga valores existentes si la estadía ya tenía tarifa personalizada.
        if (session.HasCustomRate)
        {
            cmbType.SelectedItem = session.CustomRateType;
            txtAmount.Text = session.CustomRateAmount!.Value.ToString("0.##");
            numGrace.Value = Math.Min(numGrace.Maximum, session.CustomGraceMinutes ?? 0);
            if (session.CustomBlockAmount is { } b)
            {
                chkBlock.Checked = true;
                txtBlockAmount.Text = b.ToString("0.##");
            }

            txtNote.Text = session.CustomNote ?? string.Empty;
        }

        Controls.AddRange(
        [
            accent, lblTitle, lblHint,
            MakeCaption("Unidad de tiempo", new Point(24, 100)), cmbType,
            MakeCaption("Monto por unidad", new Point(24, 162)), txtAmount,
            MakeCaption("Minutos de gracia", new Point(24, 224)), numGrace,
            chkBlock, txtBlockAmount,
            txtNote, btnSave, btnCancel
        ]);

        AcceptButton = btnSave;
        ApplyType();
    }

    private static Label MakeCaption(string text, Point location) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(71, 85, 105),
        Location = location,
        AutoSize = true
    };

    private static Button MakeButton(string text, Point location, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(178, 44),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        if (primary)
        {
            button.BackColor = Color.FromArgb(124, 58, 237);
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
