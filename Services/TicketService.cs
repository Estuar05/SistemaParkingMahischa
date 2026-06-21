using System.Drawing.Printing;
using QRCoder;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class TicketService
{
    public Image GenerateQr(Guid ticketCode, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(ticketCode.ToString(), QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(pixelsPerModule);
        using var stream = new MemoryStream(bytes);
        return Image.FromStream(stream);
    }

    public void PrintTicket(ParkingSession session)
    {
        using var document = new PrintDocument();
        document.DocumentName = $"Tiquete {session.Plate}";
        document.PrintPage += (_, e) =>
        {
            if (e.Graphics is not null)
            {
                DrawTicket(e.Graphics, session, new Rectangle(12, 12, 280, 420));
            }
        };
        document.Print();
    }

    public void DrawTicket(Graphics graphics, ParkingSession session, Rectangle bounds)
    {
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var normalFont = new Font("Segoe UI", 10, FontStyle.Regular);
        using var smallFont = new Font("Segoe UI", 8, FontStyle.Regular);
        using var contactFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var monoFont = new Font("Consolas", 10, FontStyle.Bold);
        using var pen = new Pen(Color.FromArgb(220, 226, 235));
        using var brush = new SolidBrush(Color.FromArgb(32, 43, 54));
        using var mutedBrush = new SolidBrush(Color.FromArgb(90, 100, 112));
        using var centered = new StringFormat { Alignment = StringAlignment.Center };

        var y = bounds.Top;
        graphics.DrawString(AppSettings.BusinessName, titleFont, brush, bounds.Left, y);
        y += 34;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 14;

        graphics.DrawString($"Placa: {session.Plate}", monoFont, brush, bounds.Left, y);
        y += 24;
        graphics.DrawString($"Entrada: {session.EntryAt:dd/MM/yyyy HH:mm}", normalFont, brush, bounds.Left, y);
        y += 22;
        graphics.DrawString($"Tarifa: {session.RateName}", normalFont, brush, bounds.Left, y);
        y += 22;
        graphics.DrawString($"Impreso por: {session.EnteredBy}", smallFont, brush, bounds.Left, y);
        y += 24;

        using var qrImage = GenerateQr(session.TicketCode, 7);
        graphics.DrawImage(qrImage, bounds.Left + 54, y, 150, 150);
        y += 162;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 12;
        graphics.DrawString("Conserve este tiquete para agilizar la salida.", normalFont, brush, bounds.Left, y);
        y += 24;

        var message =
            $"Estimado cliente, si tiene alguna duda puede contactarse al {AppSettings.ContactPhone} para brindarle la mejor atención.";
        var messageRect = new RectangleF(bounds.Left, y, bounds.Width, bounds.Bottom - y);
        graphics.DrawString(message, contactFont, mutedBrush, messageRect, centered);
    }
}
