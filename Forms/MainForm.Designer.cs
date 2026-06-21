namespace SistemaParkingMahischa.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Panel pnlSidebar;
    private Panel pnlTop;
    private Panel pnlContent;
    private Label lblBrand;
    private Label lblTitle;
    private Label lblUser;
    private Button btnDashboard;
    private Button btnParking;
    private Button btnRates;
    private Button btnUsers;
    private Button btnClosures;
    private Button btnLogout;

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
        pnlSidebar = new Panel();
        btnLogout = new Button();
        btnClosures = new Button();
        btnUsers = new Button();
        btnRates = new Button();
        btnParking = new Button();
        btnDashboard = new Button();
        lblBrand = new Label();
        pnlTop = new Panel();
        lblUser = new Label();
        lblTitle = new Label();
        pnlContent = new Panel();
        pnlSidebar.SuspendLayout();
        pnlTop.SuspendLayout();
        SuspendLayout();
        // 
        // pnlSidebar
        // 
        pnlSidebar.BackColor = Color.FromArgb(21, 32, 43);
        pnlSidebar.Controls.Add(btnLogout);
        pnlSidebar.Controls.Add(btnClosures);
        pnlSidebar.Controls.Add(btnUsers);
        pnlSidebar.Controls.Add(btnRates);
        pnlSidebar.Controls.Add(btnParking);
        pnlSidebar.Controls.Add(btnDashboard);
        pnlSidebar.Controls.Add(lblBrand);
        pnlSidebar.Dock = DockStyle.Left;
        pnlSidebar.Location = new Point(0, 0);
        pnlSidebar.Name = "pnlSidebar";
        pnlSidebar.Size = new Size(230, 768);
        pnlSidebar.TabIndex = 0;
        // 
        // btnLogout
        // 
        btnLogout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.FlatStyle = FlatStyle.Flat;
        btnLogout.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnLogout.ForeColor = Color.FromArgb(235, 241, 245);
        btnLogout.Location = new Point(18, 706);
        btnLogout.Name = "btnLogout";
        btnLogout.Size = new Size(194, 42);
        btnLogout.TabIndex = 6;
        btnLogout.Text = "Salir";
        btnLogout.TextAlign = ContentAlignment.MiddleLeft;
        btnLogout.UseVisualStyleBackColor = true;
        btnLogout.Click += btnLogout_Click;
        // 
        // btnClosures
        // 
        btnClosures.FlatAppearance.BorderSize = 0;
        btnClosures.FlatStyle = FlatStyle.Flat;
        btnClosures.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnClosures.ForeColor = Color.FromArgb(235, 241, 245);
        btnClosures.Location = new Point(18, 326);
        btnClosures.Name = "btnClosures";
        btnClosures.Size = new Size(194, 42);
        btnClosures.TabIndex = 5;
        btnClosures.Text = "Cierres";
        btnClosures.TextAlign = ContentAlignment.MiddleLeft;
        btnClosures.UseVisualStyleBackColor = true;
        btnClosures.Click += btnClosures_Click;
        // 
        // btnUsers
        // 
        btnUsers.FlatAppearance.BorderSize = 0;
        btnUsers.FlatStyle = FlatStyle.Flat;
        btnUsers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnUsers.ForeColor = Color.FromArgb(235, 241, 245);
        btnUsers.Location = new Point(18, 276);
        btnUsers.Name = "btnUsers";
        btnUsers.Size = new Size(194, 42);
        btnUsers.TabIndex = 4;
        btnUsers.Text = "Usuarios";
        btnUsers.TextAlign = ContentAlignment.MiddleLeft;
        btnUsers.UseVisualStyleBackColor = true;
        btnUsers.Click += btnUsers_Click;
        // 
        // btnRates
        // 
        btnRates.FlatAppearance.BorderSize = 0;
        btnRates.FlatStyle = FlatStyle.Flat;
        btnRates.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnRates.ForeColor = Color.FromArgb(235, 241, 245);
        btnRates.Location = new Point(18, 226);
        btnRates.Name = "btnRates";
        btnRates.Size = new Size(194, 42);
        btnRates.TabIndex = 3;
        btnRates.Text = "Tarifas";
        btnRates.TextAlign = ContentAlignment.MiddleLeft;
        btnRates.UseVisualStyleBackColor = true;
        btnRates.Click += btnRates_Click;
        // 
        // btnParking
        // 
        btnParking.FlatAppearance.BorderSize = 0;
        btnParking.FlatStyle = FlatStyle.Flat;
        btnParking.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnParking.ForeColor = Color.FromArgb(235, 241, 245);
        btnParking.Location = new Point(18, 176);
        btnParking.Name = "btnParking";
        btnParking.Size = new Size(194, 42);
        btnParking.TabIndex = 2;
        btnParking.Text = "Entrada / salida";
        btnParking.TextAlign = ContentAlignment.MiddleLeft;
        btnParking.UseVisualStyleBackColor = true;
        btnParking.Click += btnParking_Click;
        // 
        // btnDashboard
        // 
        btnDashboard.FlatAppearance.BorderSize = 0;
        btnDashboard.FlatStyle = FlatStyle.Flat;
        btnDashboard.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnDashboard.ForeColor = Color.FromArgb(235, 241, 245);
        btnDashboard.Location = new Point(18, 126);
        btnDashboard.Name = "btnDashboard";
        btnDashboard.Size = new Size(194, 42);
        btnDashboard.TabIndex = 1;
        btnDashboard.Text = "Panel";
        btnDashboard.TextAlign = ContentAlignment.MiddleLeft;
        btnDashboard.UseVisualStyleBackColor = true;
        btnDashboard.Click += btnDashboard_Click;
        // 
        // lblBrand
        // 
        lblBrand.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        lblBrand.ForeColor = Color.White;
        lblBrand.Location = new Point(18, 24);
        lblBrand.Name = "lblBrand";
        lblBrand.Size = new Size(194, 62);
        lblBrand.TabIndex = 0;
        lblBrand.Text = "Parking Mahischa";
        // 
        // pnlTop
        // 
        pnlTop.BackColor = Color.White;
        pnlTop.Controls.Add(lblUser);
        pnlTop.Controls.Add(lblTitle);
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Location = new Point(230, 0);
        pnlTop.Name = "pnlTop";
        pnlTop.Size = new Size(1136, 78);
        pnlTop.TabIndex = 1;
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };
        // 
        // lblUser
        // 
        lblUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblUser.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblUser.ForeColor = Color.FromArgb(71, 85, 105);
        lblUser.Location = new Point(692, 28);
        lblUser.Name = "lblUser";
        lblUser.Size = new Size(304, 22);
        lblUser.TabIndex = 1;
        lblUser.Text = "Usuario";
        lblUser.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
        lblTitle.Location = new Point(28, 22);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(75, 32);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Panel";
        // 
        // pnlContent
        // 
        pnlContent.AutoScroll = true;
        pnlContent.BackColor = Color.FromArgb(244, 247, 250);
        pnlContent.Dock = DockStyle.Fill;
        pnlContent.Location = new Point(230, 78);
        pnlContent.Name = "pnlContent";
        pnlContent.Padding = new Padding(24);
        pnlContent.Size = new Size(1136, 690);
        pnlContent.TabIndex = 2;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(1366, 768);
        Controls.Add(pnlContent);
        Controls.Add(pnlTop);
        Controls.Add(pnlSidebar);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimumSize = new Size(1180, 720);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Text = "SistemaParkingMahischa";
        pnlSidebar.ResumeLayout(false);
        pnlTop.ResumeLayout(false);
        pnlTop.PerformLayout();
        ResumeLayout(false);
    }
}
