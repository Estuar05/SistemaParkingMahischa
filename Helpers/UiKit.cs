using System.Runtime.InteropServices;

namespace SistemaParkingMahischa.Helpers;

/// <summary>
/// Utilidades de interfaz para dar un aspecto moderno: esquinas redondeadas,
/// animaciones suaves de color al pasar el mouse y entradas animadas de los formularios.
/// </summary>
public static class UiKit
{
    [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
    private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr handle);

    /// <summary>Redondea las esquinas del control y mantiene el radio al redimensionar.</summary>
    public static void RoundCorners(Control control, int radius = 10)
    {
        void Apply()
        {
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            var region = CreateRoundRectRgn(0, 0, control.Width + 1, control.Height + 1, radius, radius);
            control.Region?.Dispose();
            control.Region = Region.FromHrgn(region);
            DeleteObject(region);
        }

        control.Resize += (_, _) => Apply();
        Apply();
    }

    /// <summary>
    /// Resalta el botón al pasar el mouse usando el resaltado nativo de WinForms
    /// (instantáneo y sin parpadeos). Animar el BackColor de un botón Flat no funciona
    /// bien porque el sistema lo enmascara con MouseOverBackColor.
    /// </summary>
    public static void AttachHover(Button button, Color rest, Color hover)
    {
        button.BackColor = rest;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = hover;
    }

    /// <summary>Activa el doble búfer de un control por reflexión para eliminar parpadeos.</summary>
    public static void EnableDoubleBuffer(Control control)
    {
        var property = typeof(Control).GetProperty(
            "DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        property?.SetValue(control, true);
    }

    /// <summary>Anima suavemente el color de fondo de un control hacia un objetivo.</summary>
    public sealed class ColorFade
    {
        private readonly Control _control;
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
        private Color _target;

        public ColorFade(Control control)
        {
            _control = control;
            _timer.Tick += (_, _) =>
            {
                var current = _control.BackColor;
                var next = Color.FromArgb(
                    Step(current.R, _target.R),
                    Step(current.G, _target.G),
                    Step(current.B, _target.B));
                _control.BackColor = next;
                if (next == _target)
                {
                    _timer.Stop();
                }
            };
        }

        public void To(Color target)
        {
            _target = target;
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        private static int Step(int from, int to)
        {
            var delta = to - from;
            return Math.Abs(delta) <= 16 ? to : from + Math.Sign(delta) * 16;
        }
    }

    /// <summary>Aplica un fundido de entrada (fade-in) al mostrar el formulario.</summary>
    public static void FadeIn(Form form, double increment = 0.08, int interval = 12)
    {
        form.Opacity = 0;
        var timer = new System.Windows.Forms.Timer { Interval = interval };
        timer.Tick += (_, _) =>
        {
            form.Opacity = Math.Min(1, form.Opacity + increment);
            if (form.Opacity >= 1)
            {
                timer.Stop();
                timer.Dispose();
            }
        };
        timer.Start();
    }
}
