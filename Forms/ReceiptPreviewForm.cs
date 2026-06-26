using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Vista previa del comprobante de pago (al cobrar la salida), con opción de imprimirlo.
/// </summary>
public sealed class ReceiptPreviewForm : Form
{
    private readonly ExitReceipt _receipt;
    private readonly TicketService _ticketService = new();

    public ReceiptPreviewForm(ExitReceipt receipt)
    {
        _receipt = receipt;

        Text = "Comprobante de pago";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 560);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;
        Font = new Font("Segoe UI", 10F);

        var paper = new Panel
        {
            Location = new Point(20, 16),
            Size = new Size(300, 472),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        paper.Paint += (_, e) => _ticketService.DrawReceipt(e.Graphics, _receipt, new Rectangle(10, 10, 280, 452));

        var btnPrint = new Button
        {
            Text = "Imprimir comprobante",
            Location = new Point(20, 500),
            Size = new Size(190, 44),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 99, 235),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnPrint.FlatAppearance.BorderSize = 0;
        UiKit.RoundCorners(btnPrint, 10);
        UiKit.AttachHover(btnPrint, Color.FromArgb(36, 99, 235), Color.FromArgb(29, 78, 192));
        btnPrint.Click += (_, _) =>
        {
            try
            {
                _ticketService.PrintReceipt(_receipt);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var btnClose = new Button
        {
            Text = "Cerrar",
            Location = new Point(220, 500),
            Size = new Size(100, 44),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 52, 65),
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(202, 213, 224);
        UiKit.RoundCorners(btnClose, 10);
        UiKit.AttachHover(btnClose, Color.White, Color.FromArgb(241, 245, 249));
        btnClose.Click += (_, _) => Close();

        Controls.Add(paper);
        Controls.Add(btnPrint);
        Controls.Add(btnClose);
        AcceptButton = btnPrint;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UiKit.FadeIn(this);
    }
}
