using System.Drawing.Printing;
using QRCoder;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class TicketService
{
    // Impresora de tiquetes EPSON TM-T88V con rollo de 80 mm: el papel mide 80 mm de ancho
    // (315 centésimas de pulgada) con un área imprimible de ~72 mm (283 centésimas).
    // El largo de la página se calcula midiendo el contenido, para que el corte automático
    // quede justo al final del tiquete.
    private const int RollPaperWidth = 315;
    private const int RollMargin = 8;
    private const int RollContentWidth = 267;

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

    public void PrintTicket(ParkingSession session) =>
        PrintRollDocument($"Tiquete {session.Plate}", (graphics, bounds) => DrawTicket(graphics, session, bounds));

    public void PrintReceipt(ExitReceipt receipt) =>
        PrintRollDocument($"Comprobante {receipt.Plate}", (graphics, bounds) => DrawReceipt(graphics, receipt, bounds));

    public void PrintClosureTicket(ClosureHistoryRecord record) =>
        PrintRollDocument(record.DisplayName, (graphics, bounds) => DrawClosureTicket(graphics, record, bounds));

    /// <summary>
    /// Imprime en el rollo de 80 mm: primero mide el contenido en un lienzo con la misma
    /// escala del impresor (centésimas de pulgada) y luego fija el tamaño de página al
    /// largo exacto, de modo que la TM-T88V alimente y corte justo donde termina el tiquete.
    /// </summary>
    private static void PrintRollDocument(string documentName, Func<Graphics, Rectangle, int> draw)
    {
        int contentBottom;
        using (var canvas = new Bitmap(RollPaperWidth, 4))
        {
            canvas.SetResolution(100f, 100f);
            using var measure = Graphics.FromImage(canvas);
            contentBottom = draw(measure, new Rectangle(RollMargin, 10, RollContentWidth, 4000));
        }

        using var document = new PrintDocument();
        document.DocumentName = documentName;
        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.DefaultPageSettings.PaperSize = new PaperSize("Rollo 80 mm", RollPaperWidth, contentBottom + 20);
        document.PrintPage += (_, e) =>
        {
            if (e.Graphics is not null)
            {
                draw(e.Graphics, new Rectangle(RollMargin, 10, RollContentWidth, contentBottom + 10));
                e.HasMorePages = false;
            }
        };
        document.Print();
    }

    /// <summary>
    /// Dibuja el tiquete de un cierre (empleado o caja) para entregarlo junto con el dinero.
    /// Devuelve la posición vertical donde terminó el contenido.
    /// </summary>
    public int DrawClosureTicket(Graphics graphics, ClosureHistoryRecord record, Rectangle bounds)
    {
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var smallFont = new Font("Segoe UI", 8, FontStyle.Regular);
        using var rowFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var rowBoldFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var monoFont = new Font("Consolas", 8, FontStyle.Regular);
        using var pen = new Pen(Color.FromArgb(220, 226, 235));
        using var brush = new SolidBrush(Color.FromArgb(32, 43, 54));
        using var mutedBrush = new SolidBrush(Color.FromArgb(90, 100, 112));
        using var centered = new StringFormat { Alignment = StringAlignment.Center };
        using var right = new StringFormat { Alignment = StringAlignment.Far };

        var y = bounds.Top;
        graphics.DrawString(AppSettings.BusinessName, titleFont, brush, new RectangleF(bounds.Left, y, bounds.Width, 30), centered);
        y += 30;
        var subtitle = record.ClosureType == "Caja" ? "CIERRE DE CAJA" : "CIERRE DE EMPLEADO";
        graphics.DrawString($"{subtitle} #{record.ClosureId}", subtitleFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, 20), centered);
        y += 24;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 10;

        graphics.DrawString($"Generado: {record.CreatedAt:dd/MM/yyyy HH:mm}", smallFont, brush, bounds.Left, y);
        y += 16;
        graphics.DrawString($"Realizado por: {record.CreatedByName}", smallFont, brush, bounds.Left, y);
        y += 20;

        void Row(string label, string value, Font font)
        {
            graphics.DrawString(label, font, brush, bounds.Left, y);
            graphics.DrawString(value, font, brush, new RectangleF(bounds.Left, y, bounds.Width, 16), right);
            y += 17;
        }

        if (record.ClosureType == "Caja")
        {
            Row("Fondo de caja (esperado)", record.MinimumCashAmount.ToString("C0"), rowFont);
            Row("Contado (físico)", record.CountedAmount.ToString("C0"), rowFont);
            Row("Diferencia", record.DifferenceAmount.ToString("C0"), rowBoldFont);
        }
        else
        {
            graphics.DrawString($"Empleado: {record.EmployeeName}", smallFont, brush, bounds.Left, y);
            y += 16;
            graphics.DrawString($"Turno: {record.FromAt:dd/MM HH:mm} a {record.ToAt:dd/MM HH:mm}", smallFont, brush, bounds.Left, y);
            y += 20;
            Row("Efectivo esperado", record.ExpectedAmount.ToString("C0"), rowFont);
            Row("SINPE cobrado (aparte)", record.SinpeAmount.ToString("C0"), rowFont);
            Row("Entregado (contado)", record.DeliveredAmount.ToString("C0"), rowFont);
            Row("Diferencia", record.DifferenceAmount.ToString("C0"), rowBoldFont);
        }

        var details = record.Denominations.Where(d => d.Quantity > 0).ToList();
        if (details.Count > 0)
        {
            y += 4;
            graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
            y += 8;
            graphics.DrawString("Billetes y monedas", subtitleFont, brush, bounds.Left, y);
            y += 20;
            foreach (var detail in details)
            {
                graphics.DrawString($"{detail.Denomination,9:C0} x {detail.Quantity,4} = {detail.TotalAmount,12:C0}", monoFont, brush, bounds.Left, y);
                y += 15;
            }
        }

        y += 6;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 10;
        var footer = "Entregue este tiquete junto con el dinero.";
        var footerHeight = (int)Math.Ceiling(graphics.MeasureString(footer, smallFont, bounds.Width).Height);
        graphics.DrawString(footer, smallFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, footerHeight + 4), centered);
        return y + footerHeight + 8;
    }

    /// <summary>
    /// Dibuja el comprobante de pago (al cobrar la salida). Devuelve la posición vertical
    /// donde terminó el contenido.
    /// </summary>
    public int DrawReceipt(Graphics graphics, ExitReceipt receipt, Rectangle bounds)
    {
        using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
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
        y += 20;
        if (receipt.IsReprint)
        {
            graphics.DrawString("*** REIMPRESIÓN ***", subtitleFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, 20), centered);
            y += 20;
        }

        y += 4;
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
        else if (receipt.PaymentMethod == PaymentMethods.Mixed)
        {
            Row("Pagado en efectivo", (receipt.CashPortion ?? 0m).ToString("C0"), rowFont, brush);
            Row("Pagado por SINPE", (receipt.SinpePortion ?? 0m).ToString("C0"), rowFont, brush);
            if (!string.IsNullOrWhiteSpace(receipt.Reference))
            {
                Row("Referencia", receipt.Reference!, rowFont, brush);
            }
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
        var messageHeight = (int)Math.Ceiling(graphics.MeasureString(message, contactFont, bounds.Width).Height);
        graphics.DrawString(message, contactFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, messageHeight + 4), centered);
        return y + messageHeight + 8;
    }

    /// <summary>En estadías largas agrega el total de horas, para poder verificar el cobro.</summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m ({(int)duration.TotalHours} h)";
        }

        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }

    /// <summary>
    /// Dibuja el tiquete de entrada. Devuelve la posición vertical donde terminó el contenido.
    /// </summary>
    public int DrawTicket(Graphics graphics, ParkingSession session, Rectangle bounds)
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
        graphics.DrawImage(qrImage, bounds.Left + ((bounds.Width - 150) / 2), y, 150, 150);
        y += 162;
        graphics.DrawLine(pen, bounds.Left, y, bounds.Right, y);
        y += 12;
        graphics.DrawString("Conserve este tiquete para agilizar la salida.", normalFont, brush, bounds.Left, y);
        y += 24;

        var message =
            $"Estimado cliente, si tiene alguna duda puede contactarse al {AppSettings.ContactPhone} para brindarle la mejor atención.";
        var messageHeight = (int)Math.Ceiling(graphics.MeasureString(message, contactFont, bounds.Width).Height);
        graphics.DrawString(message, contactFont, mutedBrush, new RectangleF(bounds.Left, y, bounds.Width, messageHeight + 4), centered);
        return y + messageHeight + 8;
    }
}
