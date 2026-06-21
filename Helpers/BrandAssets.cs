using System.Drawing.Imaging;

namespace SistemaParkingMahischa.Helpers;

/// <summary>
/// Carga y reutiliza el logo del parqueo (Assets/logo.png). Si el archivo no existe,
/// todo devuelve null y la aplicación sigue funcionando sin logo.
/// </summary>
public static class BrandAssets
{
    private static bool _logoLoaded;
    private static Image? _logo;
    private static Icon? _icon;

    public static Image? Logo
    {
        get
        {
            if (!_logoLoaded)
            {
                _logoLoaded = true;
                _logo = LoadLogo();
            }

            return _logo;
        }
    }

    public static Icon? Icon
    {
        get
        {
            if (_icon is null && Logo is { } logo)
            {
                try
                {
                    using var square = new Bitmap(logo, new Size(64, 64));
                    _icon = Icon.FromHandle(square.GetHicon());
                }
                catch
                {
                    _icon = null;
                }
            }

            return _icon;
        }
    }

    /// <summary>Dibuja el logo centrado como marca de agua tenue dentro de los límites dados.</summary>
    public static void DrawWatermark(Graphics graphics, Rectangle bounds, float opacity = 0.07f)
    {
        if (Logo is not { } logo)
        {
            return;
        }

        var side = (int)(Math.Min(bounds.Width, bounds.Height) * 0.85);
        if (side <= 0)
        {
            return;
        }

        var destination = new Rectangle(
            bounds.Left + (bounds.Width - side) / 2,
            bounds.Top + (bounds.Height - side) / 2,
            side,
            side);

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        var previousMode = graphics.InterpolationMode;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(
            logo,
            destination,
            0, 0, logo.Width, logo.Height,
            GraphicsUnit.Pixel,
            attributes);
        graphics.InterpolationMode = previousMode;
    }

    private static Image? LoadLogo()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
            if (!File.Exists(path))
            {
                return null;
            }

            // Se copia a un bitmap propio para poder cerrar el archivo de inmediato.
            using var fromFile = Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
            return new Bitmap(fromFile);
        }
        catch
        {
            return null;
        }
    }
}
