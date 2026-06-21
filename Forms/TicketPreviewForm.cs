using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

public partial class TicketPreviewForm : Form
{
    private readonly ParkingSession _session;
    private readonly TicketService _ticketService = new();

    public TicketPreviewForm(ParkingSession session)
    {
        InitializeComponent();
        _session = session;
        lblPlate.Text = session.Plate;
        lblDate.Text = session.EntryAt.ToString("dd/MM/yyyy HH:mm");
        lblRate.Text = session.RateName;
        lblPrintedBy.Text = $"Impreso por: {session.EnteredBy}";
        lblContact.Text =
            $"Estimado cliente, si tiene alguna duda puede contactarse al {AppSettings.ContactPhone} para brindarle la mejor atención.";
        picQr.Image = _ticketService.GenerateQr(session.TicketCode, 8);

        btnPrint.Cursor = Cursors.Hand;
        btnClose.Cursor = Cursors.Hand;
        btnPrint.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, 99, 235);
        btnClose.FlatAppearance.MouseOverBackColor = Color.White;
        UiKit.RoundCorners(btnPrint, 10);
        UiKit.RoundCorners(btnClose, 10);
        UiKit.AttachHover(btnPrint, Color.FromArgb(36, 99, 235), Color.FromArgb(29, 78, 192));
        UiKit.AttachHover(btnClose, Color.White, Color.FromArgb(241, 245, 249));

        Icon = BrandAssets.Icon;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UiKit.FadeIn(this);
    }

    private void btnPrint_Click(object sender, EventArgs e)
    {
        try
        {
            _ticketService.PrintTicket(_session);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();
}
