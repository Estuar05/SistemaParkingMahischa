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
        // Formato "N" (32 hexadecimales sin guiones): se escanea de forma confiable en
        // cualquier distribución de teclado, evitando que el lector altere el separador "-".
        using var data = generator.CreateQrCode(ticketCode.ToString("N"), QRCodeGenerator.ECCLevel.Q);
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

    public void PrintReceipt(ExitReceipt receipt)
    {
        using var document = new PrintDocument();
        document.DocumentName = $"Comprobante {receipt.Plate}";
        document.PrintPage += (_, e) =>
        {
            if (e.Graphics is not null)
            {
                DrawReceipt(e.Graphics, receipt, new Rectangle(12, 12, 280, 460));
            }
        };
        document.Print();
    }

    /// <summary>Dibuja el comprobante de pago (al cobrar la salida).</summary>
    public void DrawReceipt(Graphics graphics, ExitReceipt receipt, Rectangle bounds)
    {
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var normalFont = new Font("Segoe UI", 10, FontStyle.Regular);
        using var smallFont = new Font("Segoe UI", 8, FontStyle.Regular);
        using var rowFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var contactFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var monoFont = new Font("Consolas", 11, FontStyle.Bold);
        using var totalFont = new Font("Segoe UI", 13, FontStyle.Bold);
        using var pen = new Pen(Color.FromArgb(220, 226, 235));
        using var brush = new SolidBrush(Color.FromArgb(32, 43, 54));
        using var mutedBrush = new SolidBrush(Color.FromArgb(90, 100, 112));
        using var accentBrush = new SolidBrush(Color.FromArgb(22, 135, 62));
        using var centered = new StringFormat { Alignment = StringAlignment.Center };
        using var right = new StringFormat { Alignment = StringAlignment.Far };

        var y = bounds.Top;
        graphics.DrawString(AppSettings.BusinessName, titleFont, brush, new RectangleF(bounds.Left, y, bounds.Width, 30), centered);
        y += 30;
        graphics.DrawString("COMPROBANTE DE PAGO", subtitleFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, 20), centered);
        y += 24;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 12;

        graphics.DrawString($"Placa: {receipt.Plate}", monoFont, brush, bounds.Left, y);
        y += 26;
        graphics.DrawString($"Entrada: {receipt.EntryAt:dd/MM/yyyy HH:mm}", smallFont, brush, bounds.Left, y);
        y += 18;
        graphics.DrawString($"Salida:  {receipt.ExitAt:dd/MM/yyyy HH:mm}", smallFont, brush, bounds.Left, y);
        y += 18;
        graphics.DrawString($"Tiempo:  {FormatDuration(receipt.ExitAt - receipt.EntryAt)}", smallFont, brush, bounds.Left, y);
        y += 18;
        graphics.DrawString($"Tarifa:  {receipt.RateName}", smallFont, brush, bounds.Left, y);
        y += 22;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 10;

        void Row(string label, string value, Font font, Brush colorBrush)
        {
            graphics.DrawString(label, font, colorBrush, bounds.Left, y);
            graphics.DrawString(value, font, colorBrush, new RectangleF(bounds.Left, y, bounds.Width, 18), right);
            y += 20;
        }

        Row("Monto por tiempo", receipt.BaseAmount.ToString("C0"), rowFont, brush);
        if (receipt.ExtraAmount > 0)
        {
            Row("Monto extra por tiempo adicional", receipt.ExtraAmount.ToString("C0"), rowFont, brush);
        }

        y += 4;
        graphics.DrawString("TOTAL A PAGAR", subtitleFont, accentBrush, bounds.Left, y + 4);
        graphics.DrawString(receipt.Total.ToString("C0"), totalFont, accentBrush, new RectangleF(bounds.Left, y, bounds.Width, 24), right);
        y += 30;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 10;

        Row("Forma de pago", receipt.PaymentMethod, rowFont, brush);
        if (receipt.PaymentMethod == PaymentMethods.Cash && receipt.TenderedAmount is { } tendered)
        {
            Row("Paga con", tendered.ToString("C0"), rowFont, brush);
            Row("Vuelto", (receipt.ChangeAmount ?? 0m).ToString("C0"), rowFont, brush);
        }
        else if (receipt.PaymentMethod == PaymentMethods.Sinpe && !string.IsNullOrWhiteSpace(receipt.Reference))
        {
            Row("Referencia", receipt.Reference!, rowFont, brush);
        }

        y += 4;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 10;
        graphics.DrawString($"Atendido por: {receipt.CashierName}", smallFont, brush, bounds.Left, y);
        y += 24;

        var message =
            $"Gracias por su preferencia. Cualquier consulta al {AppSettings.ContactPhone}.";
        var messageRect = new RectangleF(bounds.Left, y, bounds.Width, Math.Max(0, bounds.Bottom - y));
        graphics.DrawString(message, contactFont, mutedBrush, messageRect, centered);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        }

        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
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
