namespace SistemaParkingMahischa.Forms;

partial class TicketPreviewForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblTitle;
    private Label lblPlateCaption;
    private Label lblPlate;
    private Label lblDateCaption;
    private Label lblDate;
    private Label lblRateCaption;
    private Label lblRate;
    private PictureBox picQr;
    private Label lblPrintedBy;
    private Label lblContact;
    private Button btnPrint;
    private Button btnClose;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            picQr.Image?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblPlateCaption = new Label();
        lblPlate = new Label();
        lblDateCaption = new Label();
        lblDate = new Label();
        lblRateCaption = new Label();
        lblRate = new Label();
        picQr = new PictureBox();
        lblPrintedBy = new Label();
        lblContact = new Label();
        btnPrint = new Button();
        btnClose = new Button();
        ((System.ComponentModel.ISupportInitialize)picQr).BeginInit();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(15, 23, 42);
        lblTitle.Location = new Point(32, 24);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(190, 32);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Tiquete de entrada";
        // 
        // lblPlateCaption
        // 
        lblPlateCaption.AutoSize = true;
        lblPlateCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblPlateCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblPlateCaption.Location = new Point(34, 82);
        lblPlateCaption.Name = "lblPlateCaption";
        lblPlateCaption.Size = new Size(36, 15);
        lblPlateCaption.TabIndex = 1;
        lblPlateCaption.Text = "Placa";
        // 
        // lblPlate
        // 
        lblPlate.AutoSize = true;
        lblPlate.Font = new Font("Consolas", 18F, FontStyle.Bold);
        lblPlate.ForeColor = Color.FromArgb(36, 99, 235);
        lblPlate.Location = new Point(34, 102);
        lblPlate.Name = "lblPlate";
        lblPlate.Size = new Size(77, 28);
        lblPlate.TabIndex = 2;
        lblPlate.Text = "ABC123";
        // 
        // lblDateCaption
        // 
        lblDateCaption.AutoSize = true;
        lblDateCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblDateCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblDateCaption.Location = new Point(34, 150);
        lblDateCaption.Name = "lblDateCaption";
        lblDateCaption.Size = new Size(50, 15);
        lblDateCaption.TabIndex = 3;
        lblDateCaption.Text = "Entrada";
        // 
        // lblDate
        // 
        lblDate.AutoSize = true;
        lblDate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblDate.ForeColor = Color.FromArgb(30, 41, 59);
        lblDate.Location = new Point(34, 170);
        lblDate.Name = "lblDate";
        lblDate.Size = new Size(142, 20);
        lblDate.TabIndex = 4;
        lblDate.Text = "16/06/2026 08:00";
        // 
        // lblRateCaption
        // 
        lblRateCaption.AutoSize = true;
        lblRateCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblRateCaption.ForeColor = Color.FromArgb(100, 116, 139);
        lblRateCaption.Location = new Point(34, 210);
        lblRateCaption.Name = "lblRateCaption";
        lblRateCaption.Size = new Size(38, 15);
        lblRateCaption.TabIndex = 5;
        lblRateCaption.Text = "Tarifa";
        // 
        // lblRate
        // 
        lblRate.AutoSize = true;
        lblRate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblRate.ForeColor = Color.FromArgb(30, 41, 59);
        lblRate.Location = new Point(34, 230);
        lblRate.Name = "lblRate";
        lblRate.Size = new Size(67, 20);
        lblRate.TabIndex = 6;
        lblRate.Text = "Por hora";
        // 
        // picQr
        // 
        picQr.Location = new Point(259, 82);
        picQr.Name = "picQr";
        picQr.Size = new Size(210, 210);
        picQr.SizeMode = PictureBoxSizeMode.Zoom;
        picQr.TabIndex = 7;
        picQr.TabStop = false;
        // 
        // lblPrintedBy
        // 
        lblPrintedBy.Font = new Font("Segoe UI", 8F);
        lblPrintedBy.ForeColor = Color.FromArgb(71, 85, 105);
        lblPrintedBy.Location = new Point(34, 270);
        lblPrintedBy.Name = "lblPrintedBy";
        lblPrintedBy.Size = new Size(190, 28);
        lblPrintedBy.TabIndex = 8;
        lblPrintedBy.Text = "Impreso por:";
        lblPrintedBy.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblContact
        //
        lblContact.Font = new Font("Segoe UI", 8.5F);
        lblContact.ForeColor = Color.FromArgb(100, 116, 139);
        lblContact.Location = new Point(34, 302);
        lblContact.Name = "lblContact";
        lblContact.Size = new Size(438, 46);
        lblContact.TabIndex = 9;
        lblContact.Text = "Estimado cliente...";
        lblContact.TextAlign = ContentAlignment.TopLeft;
        //
        // btnPrint
        //
        btnPrint.BackColor = Color.FromArgb(36, 99, 235);
        btnPrint.FlatAppearance.BorderSize = 0;
        btnPrint.FlatStyle = FlatStyle.Flat;
        btnPrint.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnPrint.ForeColor = Color.White;
        btnPrint.Location = new Point(128, 360);
        btnPrint.Name = "btnPrint";
        btnPrint.Size = new Size(150, 42);
        btnPrint.TabIndex = 0;
        btnPrint.Text = "Imprimir";
        btnPrint.UseVisualStyleBackColor = false;
        btnPrint.Click += btnPrint_Click;
        // 
        // btnClose
        // 
        btnClose.BackColor = Color.White;
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btnClose.FlatStyle = FlatStyle.Flat;
        btnClose.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnClose.ForeColor = Color.FromArgb(51, 65, 85);
        btnClose.Location = new Point(284, 360);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(150, 42);
        btnClose.TabIndex = 1;
        btnClose.Text = "Cerrar";
        btnClose.UseVisualStyleBackColor = false;
        btnClose.Click += btnClose_Click;
        // 
        // TicketPreviewForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(506, 430);
        Controls.Add(btnClose);
        Controls.Add(btnPrint);
        Controls.Add(lblContact);
        Controls.Add(lblPrintedBy);
        Controls.Add(picQr);
        Controls.Add(lblRate);
        Controls.Add(lblRateCaption);
        Controls.Add(lblDate);
        Controls.Add(lblDateCaption);
        Controls.Add(lblPlate);
        Controls.Add(lblPlateCaption);
        Controls.Add(lblTitle);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "TicketPreviewForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Tiquete con QR";
        ((System.ComponentModel.ISupportInitialize)picQr).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
