namespace SistemaParkingMahischa.Forms;

partial class LoginForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlCard;
    private Label lblBusiness;
    private Label lblSubtitle;
    private Label lblUser;
    private TextBox txtUser;
    private Label lblPassword;
    private TextBox txtPassword;
    private Button btnLogin;
    private Button btnClose;
    private Label lblError;
    private Panel pnlAccent;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlCard = new Panel();
        pnlAccent = new Panel();
        lblBusiness = new Label();
        lblSubtitle = new Label();
        lblUser = new Label();
        txtUser = new TextBox();
        lblPassword = new Label();
        txtPassword = new TextBox();
        btnLogin = new Button();
        btnClose = new Button();
        lblError = new Label();
        pnlCard.SuspendLayout();
        SuspendLayout();
        // 
        // pnlCard
        // 
        pnlCard.BackColor = Color.White;
        pnlCard.Controls.Add(pnlAccent);
        pnlCard.Controls.Add(lblBusiness);
        pnlCard.Controls.Add(lblSubtitle);
        pnlCard.Controls.Add(lblUser);
        pnlCard.Controls.Add(txtUser);
        pnlCard.Controls.Add(lblPassword);
        pnlCard.Controls.Add(txtPassword);
        pnlCard.Controls.Add(btnLogin);
        pnlCard.Controls.Add(btnClose);
        pnlCard.Controls.Add(lblError);
        pnlCard.Dock = DockStyle.Fill;
        pnlCard.Location = new Point(0, 0);
        pnlCard.Name = "pnlCard";
        pnlCard.Size = new Size(440, 472);
        pnlCard.TabIndex = 0;
        //
        // pnlAccent
        //
        pnlAccent.BackColor = Color.FromArgb(36, 99, 235);
        pnlAccent.Dock = DockStyle.Top;
        pnlAccent.Location = new Point(0, 0);
        pnlAccent.Name = "pnlAccent";
        pnlAccent.Size = new Size(440, 6);
        pnlAccent.TabIndex = 9;
        //
        // lblBusiness
        //
        lblBusiness.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblBusiness.ForeColor = Color.FromArgb(22, 32, 46);
        lblBusiness.Location = new Point(20, 94);
        lblBusiness.Name = "lblBusiness";
        lblBusiness.Size = new Size(400, 38);
        lblBusiness.TabIndex = 0;
        lblBusiness.Text = "Parqueo Mahischa";
        lblBusiness.TextAlign = ContentAlignment.MiddleCenter;
        //
        // lblSubtitle
        //
        lblSubtitle.Font = new Font("Segoe UI", 10F);
        lblSubtitle.ForeColor = Color.FromArgb(101, 116, 139);
        lblSubtitle.Location = new Point(20, 134);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(400, 20);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Sistema local de entradas y salidas";
        lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
        //
        // lblUser
        //
        lblUser.AutoSize = true;
        lblUser.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUser.ForeColor = Color.FromArgb(45, 55, 72);
        lblUser.Location = new Point(40, 178);
        lblUser.Name = "lblUser";
        lblUser.Size = new Size(52, 19);
        lblUser.TabIndex = 2;
        lblUser.Text = "Cedula";
        //
        // txtUser
        //
        txtUser.BorderStyle = BorderStyle.FixedSingle;
        txtUser.Font = new Font("Segoe UI", 12F);
        txtUser.Location = new Point(40, 202);
        txtUser.MaxLength = 50;
        txtUser.Name = "txtUser";
        txtUser.Size = new Size(360, 30);
        txtUser.TabIndex = 0;
        //
        // lblPassword
        //
        lblPassword.AutoSize = true;
        lblPassword.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblPassword.ForeColor = Color.FromArgb(45, 55, 72);
        lblPassword.Location = new Point(40, 252);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(83, 19);
        lblPassword.TabIndex = 4;
        lblPassword.Text = "Contraseña";
        //
        // txtPassword
        //
        txtPassword.BorderStyle = BorderStyle.FixedSingle;
        txtPassword.Font = new Font("Segoe UI", 12F);
        txtPassword.Location = new Point(40, 276);
        txtPassword.MaxLength = 80;
        txtPassword.Name = "txtPassword";
        txtPassword.PasswordChar = '•';
        txtPassword.Size = new Size(360, 30);
        txtPassword.TabIndex = 1;
        //
        // btnLogin
        //
        btnLogin.BackColor = Color.FromArgb(36, 99, 235);
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.FlatStyle = FlatStyle.Flat;
        btnLogin.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        btnLogin.ForeColor = Color.White;
        btnLogin.Location = new Point(40, 336);
        btnLogin.Name = "btnLogin";
        btnLogin.Size = new Size(360, 46);
        btnLogin.TabIndex = 2;
        btnLogin.Text = "Ingresar";
        btnLogin.UseVisualStyleBackColor = false;
        btnLogin.Click += btnLogin_Click;
        //
        // btnClose
        //
        btnClose.BackColor = Color.White;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClose.FlatStyle = FlatStyle.Flat;
        btnClose.Font = new Font("Segoe UI", 10F);
        btnClose.ForeColor = Color.FromArgb(71, 85, 105);
        btnClose.Location = new Point(40, 390);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(360, 40);
        btnClose.TabIndex = 3;
        btnClose.Text = "Salir";
        btnClose.UseVisualStyleBackColor = false;
        btnClose.Click += btnClose_Click;
        //
        // lblError
        //
        lblError.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblError.ForeColor = Color.FromArgb(220, 38, 38);
        lblError.Location = new Point(40, 312);
        lblError.Name = "lblError";
        lblError.Size = new Size(360, 22);
        lblError.TabIndex = 8;
        lblError.TextAlign = ContentAlignment.MiddleCenter;
        //
        // LoginForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(440, 472);
        Controls.Add(pnlCard);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Parqueo Mahischa";
        pnlCard.ResumeLayout(false);
        pnlCard.PerformLayout();
        ResumeLayout(false);
    }
}
