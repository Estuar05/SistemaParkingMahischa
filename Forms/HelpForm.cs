using SistemaParkingMahischa.Helpers;

namespace SistemaParkingMahischa.Forms;

/// <summary>Ventana de ayuda con instrucciones detalladas de un módulo.</summary>
public sealed class HelpForm : Form
{
    public HelpForm(string moduleTitle, string body)
    {
        Text = "Ayuda";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(580, 580);
        MinimumSize = new Size(440, 380);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;

        var header = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.FromArgb(36, 99, 235) };
        var lblTitle = new Label
        {
            Text = "Cómo usar: " + moduleTitle,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0)
        };
        header.Controls.Add(lblTitle);

        var bodyBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            Font = new Font("Segoe UI", 10.5F),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(30, 41, 59),
            Text = body
        };
        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 16, 18, 12), BackColor = Color.White };
        bodyPanel.Controls.Add(bodyBox);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = Color.White };
        var btnClose = new Button
        {
            Text = "Entendido",
            Size = new Size(150, 42),
            Location = new Point(footer.Width - 150 - 18, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 128, 117),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 96, 88);
        btnClose.Click += (_, _) => Close();
        UiKit.RoundCorners(btnClose, 8);
        footer.Controls.Add(btnClose);

        Controls.Add(bodyPanel);
        Controls.Add(footer);
        Controls.Add(header);
        AcceptButton = btnClose;
    }
}
