using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Controllers;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Forms;

public partial class LoginForm : Form
{
    private readonly LoginController _controller = new();
    private readonly System.Windows.Forms.Timer _fadeTimer = new();

    public User? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        InitializeComponent();
        Opacity = 0;
        lblBusiness.Text = AppSettings.BusinessName;
        AcceptButton = btnLogin;

        UiKit.RoundCorners(this, 18);
        btnLogin.Cursor = Cursors.Hand;
        btnClose.Cursor = Cursors.Hand;
        UiKit.RoundCorners(btnLogin, 10);
        UiKit.RoundCorners(btnClose, 10);
        UiKit.AttachHover(btnLogin, Color.FromArgb(36, 99, 235), Color.FromArgb(29, 78, 192));
        UiKit.AttachHover(btnClose, Color.White, Color.FromArgb(241, 245, 249));

        Icon = BrandAssets.Icon;
        if (BrandAssets.Logo is { } logo)
        {
            var picLogo = new PictureBox
            {
                Image = logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(60, 60),
                Location = new Point((pnlCard.Width - 60) / 2, 22),
                BackColor = Color.Transparent
            };
            pnlCard.Controls.Add(picLogo);
            picLogo.BringToFront();
            EnableDrag(picLogo);
        }

        // La ventana no tiene borde: permitir arrastrarla desde la parte superior de la tarjeta.
        EnableDrag(pnlCard);
        EnableDrag(lblBusiness);
        EnableDrag(lblSubtitle);

        _fadeTimer.Interval = 12;
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity = Math.Min(1, Opacity + 0.06);
            if (Opacity >= 1)
            {
                _fadeTimer.Stop();
            }
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _fadeTimer.Start();
    }

    private void btnLogin_Click(object sender, EventArgs e)
    {
        if (!btnLogin.Enabled)
        {
            return;
        }

        btnLogin.Enabled = false;
        lblError.Text = string.Empty;

        try
        {
            AuthenticatedUser = _controller.Login(txtUser.Text, txtPassword.Text);
            if (AuthenticatedUser is null)
            {
                lblError.Text = "Usuario o contraseña inválidos.";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;
        }
        finally
        {
            if (DialogResult != DialogResult.OK)
            {
                var timer = new System.Windows.Forms.Timer { Interval = 5000 };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!btnLogin.IsDisposed)
                    {
                        btnLogin.Enabled = true;
                    }
                };
                timer.Start();
            }
        }
    }

    private void btnClose_Click(object sender, EventArgs e) => Close();

    // Sombra sutil para la tarjeta sin borde.
    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_DROPSHADOW = 0x20000;
            var createParams = base.CreateParams;
            createParams.ClassStyle |= CS_DROPSHADOW;
            return createParams;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private void EnableDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                const int WM_NCLBUTTONDOWN = 0xA1;
                const int HT_CAPTION = 0x2;
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        };
    }
}
