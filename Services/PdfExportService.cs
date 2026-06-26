using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class PdfExportService
{
    private static readonly XFont TitleFont;
    private static readonly XFont HeaderFont;
    private static readonly XFont NormalFont;
    private static readonly XFont SmallFont;

    static PdfExportService()
    {
        // El resolver debe quedar registrado ANTES de crear cualquier XFont.
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
        }

        TitleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
        HeaderFont = new XFont("Arial", 11, XFontStyleEx.Bold);
        NormalFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        SmallFont = new XFont("Arial", 9, XFontStyleEx.Regular);
    }

    public string BuildDefaultFileName(ClosureHistoryRecord record)
    {
        var name = record.ClosureType == "Caja"
            ? $"Cierre_Caja_{record.CreatedByName}_{record.CreatedAt:yyyyMMdd_HHmm}"
            : $"Cierre_Empleado_{record.EmployeeName}_{record.CreatedAt:yyyyMMdd_HHmm}";

        return Sanitize(name) + ".pdf";
    }

    public void ExportClosure(ClosureHistoryRecord record, string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = record.DisplayName;
        var page = NewPage(document, out var gfx);
        var y = DrawTitle(gfx, page, record.DisplayName);

        if (record.ClosureType == "Caja")
        {
            DrawLine(gfx, "Realizado por", record.CreatedByName, ref y);
            DrawLine(gfx, "Fecha de cierre", record.CreatedAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, "Efectivo (sistema)", record.CashAmount.ToString("C0"), ref y);
            DrawLine(gfx, "SINPE (sistema)", record.SinpeAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Fondo de caja", record.MinimumCashAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Esperado en caja", (record.CashAmount + record.MinimumCashAmount).ToString("C0"), ref y);
            DrawLine(gfx, "Contado fisico", record.CountedAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Diferencia", record.DifferenceAmount.ToString("C0"), ref y);
            y += 14;
            gfx.DrawString("Detalle de billetes y monedas", HeaderFont, XBrushes.Black, 48, y);
            y += 22;
            foreach (var detail in record.Denominations)
            {
                gfx.DrawString($"{detail.Denomination:C0} x {detail.Quantity} = {detail.TotalAmount:C0}", SmallFont, XBrushes.Black, 64, y);
                y += 18;
            }
        }
        else
        {
            DrawLine(gfx, "Empleado", record.EmployeeName, ref y);
            DrawLine(gfx, "Realizado por", record.CreatedByName, ref y);
            DrawLine(gfx, "Desde", record.FromAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, "Hasta", record.ToAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, "Efectivo cobrado", record.CashAmount.ToString("C0"), ref y);
            DrawLine(gfx, "SINPE cobrado", record.SinpeAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Esperado (efectivo)", record.ExpectedAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Entregado (contado)", record.DeliveredAmount.ToString("C0"), ref y);
            DrawLine(gfx, "Diferencia", record.DifferenceAmount.ToString("C0"), ref y);

            if (record.Denominations.Count > 0)
            {
                y += 14;
                gfx.DrawString("Detalle de billetes y monedas entregados", HeaderFont, XBrushes.Black, 48, y);
                y += 22;
                foreach (var detail in record.Denominations)
                {
                    gfx.DrawString($"{detail.Denomination:C0} x {detail.Quantity} = {detail.TotalAmount:C0}", SmallFont, XBrushes.Black, 64, y);
                    y += 18;
                }
            }
        }

        document.Save(path);
    }

    /// <summary>Reporte de cierres en un rango de fechas (uno por línea, con totales).</summary>
    public void ExportClosureRange(IReadOnlyList<ClosureHistoryRecord> records, DateTime fromDate, DateTime toDate, string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Cierres";
        var page = NewPage(document, out var gfx);
        var y = DrawTitle(gfx, page, $"Reporte de cierres  ·  {fromDate:dd/MM/yyyy} a {toDate:dd/MM/yyyy}");

        gfx.DrawString("Tipo", HeaderFont, XBrushes.Black, 48, y);
        gfx.DrawString("Fecha", HeaderFont, XBrushes.Black, 130, y);
        gfx.DrawString("Nombre", HeaderFont, XBrushes.Black, 250, y);
        gfx.DrawString("Esperado", HeaderFont, XBrushes.Black, 400, y);
        gfx.DrawString("Diferencia", HeaderFont, XBrushes.Black, 490, y);
        y += 18;
        gfx.DrawLine(XPens.LightGray, 48, y, page.Width.Point - 48, y);
        y += 16;

        decimal totalDiff = 0;
        foreach (var record in records)
        {
            EnsureSpace(document, ref page, ref gfx, ref y);
            var name = record.ClosureType == "Caja" ? record.CreatedByName : record.EmployeeName;
            var expected = record.ClosureType == "Caja" ? record.CashAmount + record.MinimumCashAmount : record.ExpectedAmount;
            gfx.DrawString(record.ClosureType, NormalFont, XBrushes.Black, 48, y);
            gfx.DrawString(record.CreatedAt.ToString("dd/MM/yy HH:mm"), NormalFont, XBrushes.Black, 130, y);
            gfx.DrawString(Truncate(name, 24), NormalFont, XBrushes.Black, 250, y);
            gfx.DrawString(expected.ToString("C0"), NormalFont, XBrushes.Black, 400, y);
            gfx.DrawString(record.DifferenceAmount.ToString("C0"), NormalFont, XBrushes.Black, 490, y);
            totalDiff += record.DifferenceAmount;
            y += 18;
        }

        y += 10;
        gfx.DrawLine(XPens.LightGray, 48, y, page.Width.Point - 48, y);
        y += 20;
        gfx.DrawString($"Cierres: {records.Count}", HeaderFont, XBrushes.Black, 48, y);
        gfx.DrawString($"Diferencia total: {totalDiff:C0}", HeaderFont, XBrushes.Black, 300, y);

        document.Save(path);
    }

    /// <summary>Reporte de ingresos en un rango de fechas con desglose por forma de pago.</summary>
    public void ExportIncomeRange(IReadOnlyList<IncomeRecord> records, IncomeSummary summary, DateTime fromDate, DateTime toDate, string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Ingresos";
        var page = NewPage(document, out var gfx);
        var y = DrawTitle(gfx, page, $"Reporte de ingresos  ·  {fromDate:dd/MM/yyyy} a {toDate:dd/MM/yyyy}");

        DrawLine(gfx, "Efectivo", summary.Cash.ToString("C0"), ref y);
        DrawLine(gfx, "SINPE", summary.Sinpe.ToString("C0"), ref y);
        DrawLine(gfx, "Total", summary.Total.ToString("C0"), ref y);
        DrawLine(gfx, "Cobros", summary.Count.ToString(), ref y);
        y += 16;

        gfx.DrawString("Fecha", HeaderFont, XBrushes.Black, 48, y);
        gfx.DrawString("Placa", HeaderFont, XBrushes.Black, 150, y);
        gfx.DrawString("Tarifa", HeaderFont, XBrushes.Black, 230, y);
        gfx.DrawString("Método", HeaderFont, XBrushes.Black, 360, y);
        gfx.DrawString("Monto", HeaderFont, XBrushes.Black, 470, y);
        gfx.DrawString("Usuario", HeaderFont, XBrushes.Black, 540, y);
        y += 18;
        gfx.DrawLine(XPens.LightGray, 48, y, page.Width.Point - 48, y);
        y += 16;

        foreach (var record in records)
        {
            EnsureSpace(document, ref page, ref gfx, ref y);
            gfx.DrawString(record.PaidAt.ToString("dd/MM/yy HH:mm"), SmallFont, XBrushes.Black, 48, y);
            gfx.DrawString(Truncate(record.Plate, 12), SmallFont, XBrushes.Black, 150, y);
            gfx.DrawString(Truncate(record.IsCustom ? "Personalizada" : record.RateName, 20), SmallFont, XBrushes.Black, 230, y);
            gfx.DrawString(record.PaymentMethod, SmallFont, XBrushes.Black, 360, y);
            gfx.DrawString(record.Amount.ToString("C0"), SmallFont, XBrushes.Black, 470, y);
            gfx.DrawString(Truncate(record.Username, 14), SmallFont, XBrushes.Black, 540, y);
            y += 16;
        }

        document.Save(path);
    }

    private static PdfPage NewPage(PdfDocument document, out XGraphics gfx)
    {
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.Letter;
        gfx = XGraphics.FromPdfPage(page);
        return page;
    }

    private static double DrawTitle(XGraphics gfx, PdfPage page, string subtitle)
    {
        var y = 48d;
        gfx.DrawString(AppSettings.BusinessName, TitleFont, XBrushes.Black, 48, y);
        y += 32;
        gfx.DrawString(subtitle, HeaderFont, XBrushes.Black, 48, y);
        y += 24;
        gfx.DrawLine(XPens.LightGray, 48, y, page.Width.Point - 48, y);
        return y + 26;
    }

    private static void EnsureSpace(PdfDocument document, ref PdfPage page, ref XGraphics gfx, ref double y)
    {
        if (y < page.Height.Point - 60)
        {
            return;
        }

        gfx.Dispose();
        page = NewPage(document, out gfx);
        y = 60d;
    }

    private static void DrawLine(XGraphics gfx, string label, string value, ref double y)
    {
        gfx.DrawString($"{label}:", NormalFont, XBrushes.DimGray, 64, y);
        gfx.DrawString(value, NormalFont, XBrushes.Black, 240, y);
        y += 22;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private static string Sanitize(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return name;
    }
}

internal sealed class WindowsFontResolver : IFontResolver
{
    public byte[]? GetFont(string faceName)
    {
        var fileName = faceName switch
        {
            "Arial#Bold" => "arialbd.ttf",
            _ => "arial.ttf"
        };

        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(isBold ? "Arial#Bold" : "Arial#Regular");
    }
}
