using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class PdfExportService
{
    static PdfExportService()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
        }
    }

    public string BuildDefaultFileName(ClosureHistoryRecord record)
    {
        var name = record.ClosureType == "Caja"
            ? $"Cierre_Cajas_{record.CreatedByName}_{record.CreatedAt:yyyyMMdd_HHmm}"
            : $"Cierre_Dia_{record.EmployeeName}_{record.CreatedAt:yyyyMMdd_HHmm}";

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return $"{name}.pdf";
    }

    public void ExportClosure(ClosureHistoryRecord record, string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = record.DisplayName;
        var page = document.AddPage();
        page.Size = PdfSharp.PageSize.Letter;

        using var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
        var headerFont = new XFont("Arial", 11, XFontStyleEx.Bold);
        var normalFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var smallFont = new XFont("Arial", 9, XFontStyleEx.Regular);
        var brush = XBrushes.Black;
        var y = 48d;

        gfx.DrawString(AppSettings.BusinessName, titleFont, brush, 48, y);
        y += 34;
        gfx.DrawString(record.DisplayName, headerFont, brush, 48, y);
        y += 28;
        gfx.DrawLine(XPens.LightGray, 48, y, page.Width.Point - 48, y);
        y += 28;

        if (record.ClosureType == "Caja")
        {
            DrawLine(gfx, normalFont, "Realizado por", record.CreatedByName, ref y);
            DrawLine(gfx, normalFont, "Fecha de cierre", record.CreatedAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, normalFont, "Sistema", record.SystemAmount.ToString("C0"), ref y);
            DrawLine(gfx, normalFont, "Minimo de caja", record.MinimumCashAmount.ToString("C0"), ref y);
            DrawLine(gfx, normalFont, "Contado fisico", record.CountedAmount.ToString("C0"), ref y);
            DrawLine(gfx, normalFont, "Diferencia", record.DifferenceAmount.ToString("C0"), ref y);
            y += 20;

            gfx.DrawString("Detalle de billetes y monedas", headerFont, brush, 48, y);
            y += 24;
            foreach (var detail in record.Denominations)
            {
                gfx.DrawString($"{detail.Denomination:C0} x {detail.Quantity} = {detail.TotalAmount:C0}", smallFont, brush, 64, y);
                y += 18;
            }
        }
        else
        {
            DrawLine(gfx, normalFont, "Empleado", record.EmployeeName, ref y);
            DrawLine(gfx, normalFont, "Realizado por", record.CreatedByName, ref y);
            DrawLine(gfx, normalFont, "Desde", record.FromAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, normalFont, "Hasta", record.ToAt.ToString("dd/MM/yyyy HH:mm"), ref y);
            DrawLine(gfx, normalFont, "Esperado", record.ExpectedAmount.ToString("C0"), ref y);
            DrawLine(gfx, normalFont, "Entregado", record.DeliveredAmount.ToString("C0"), ref y);
            DrawLine(gfx, normalFont, "Diferencia", record.DifferenceAmount.ToString("C0"), ref y);
        }

        document.Save(path);
    }

    private static void DrawLine(XGraphics gfx, XFont font, string label, string value, ref double y)
    {
        gfx.DrawString($"{label}:", font, XBrushes.DimGray, 64, y);
        gfx.DrawString(value, font, XBrushes.Black, 220, y);
        y += 22;
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
