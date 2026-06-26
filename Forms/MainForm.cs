using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Controllers;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

public partial class MainForm : Form
{
    private readonly ParkingController _controller = new();
    private readonly User _currentUser;
    private readonly Color _accent = Color.FromArgb(0, 128, 117);
    private readonly Color _accentDark = Color.FromArgb(0, 96, 88);
    private readonly Color _muted = Color.FromArgb(91, 106, 123);
    private readonly Color _sidebar = Color.FromArgb(21, 32, 43);
    private readonly Color _sidebarHover = Color.FromArgb(33, 47, 62);
    private readonly PdfExportService _pdfExportService = new();

    // Paneles de cada módulo: se construyen una sola vez y se muestran/ocultan, de modo que la
    // información escrita (cierres, placas, etc.) no se pierde al cambiar de módulo.
    private readonly Dictionary<string, Control> _modulePanels = new();
    private readonly Dictionary<string, Action> _moduleRefreshers = new();

    private Button? _btnIncome;
    private Panel? _navIndicator;
    private readonly System.Windows.Forms.Timer _indicatorTimer = new() { Interval = 12 };
    private int _indicatorTarget;
    private Button? _activeMenuButton;
    private Button? _btnUpdate;
    private string _currentHelpKey = "Panel";

    private static readonly decimal[] DenominationValues = [20000m, 10000m, 5000m, 2000m, 1000m, 500m, 100m, 50m, 25m, 10m, 5m];

    public MainForm(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        lblUser.Text = $"{currentUser.FullName} · {currentUser.RoleName}";
        lblBrand.Text = AppSettings.BusinessName;

        btnParking.Enabled = currentUser.HasPermission(PermissionKeys.Parking);
        btnRates.Enabled = currentUser.HasPermission(PermissionKeys.Rates);
        btnUsers.Enabled = currentUser.HasPermission(PermissionKeys.Users);
        btnClosures.Enabled = currentUser.HasPermission(PermissionKeys.EmployeeClosure)
            || currentUser.HasPermission(PermissionKeys.CashClosure)
            || currentUser.HasPermission(PermissionKeys.ClosureHistory);

        Icon = BrandAssets.Icon;
        SetupBrandLogo();
        SetupNavigation();
        SetupBackupButton();
        SetupUpdateButton();
        SetupHelpButton();
        ShowDashboard();
    }

    private void SetupHelpButton()
    {
        var btnHelp = new Button
        {
            Text = "?  Ayuda",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(36, 99, 235),
            Size = new Size(120, 38),
            Location = new Point(300, 21),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnHelp.FlatAppearance.BorderSize = 0;
        btnHelp.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 192);
        UiKit.RoundCorners(btnHelp, 8);
        btnHelp.Click += (_, _) =>
        {
            using var help = new HelpForm(_currentHelpKey, HelpContent.For(_currentHelpKey));
            help.ShowDialog(this);
        };
        pnlTop.Controls.Add(btnHelp);
        btnHelp.BringToFront();
    }

    private void SetupUpdateButton()
    {
        _btnUpdate = new Button
        {
            Text = "Actualizar",
            Visible = false,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(245, 158, 11),
            Size = new Size(240, 40),
            Location = new Point(lblUser.Left - 256, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _btnUpdate.FlatAppearance.BorderSize = 0;
        _btnUpdate.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 119, 6);
        UiKit.RoundCorners(_btnUpdate, 8);
        _btnUpdate.Click += UpdateButtonClick;
        pnlTop.Controls.Add(_btnUpdate);
        _btnUpdate.BringToFront();
    }

    private async void CheckForUpdates()
    {
        var info = await UpdateService.CheckAsync();
        if (info is null || _btnUpdate is null || _btnUpdate.IsDisposed)
        {
            return;
        }

        _btnUpdate.Text = $"⟳  Actualizar a {info.Tag}";
        _btnUpdate.Tag = info;
        _btnUpdate.Visible = true;
    }

    private async void UpdateButtonClick(object? sender, EventArgs e)
    {
        if (_btnUpdate?.Tag is not UpdateInfo info)
        {
            return;
        }

        var notes = string.IsNullOrWhiteSpace(info.Notes)
            ? string.Empty
            : "\n\nNovedades:\n" + string.Join('\n', info.Notes.Split('\n').Take(6));
        var confirm = MessageBox.Show(
            $"Hay una nueva versión disponible ({info.Tag}).{notes}\n\nLa aplicación se cerrará, se actualizará y volverá a abrir automáticamente. ¿Desea continuar?",
            "Actualización disponible",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _btnUpdate.Enabled = false;
            _btnUpdate.Text = "Descargando actualización...";
            UseWaitCursor = true;
            AuditService.Log(_currentUser.UserId, "Actualizacion", "Sistema", info.Tag,
                $"De {UpdateService.CurrentVersion} a {info.Version}");
            await UpdateService.DownloadAndApplyAsync(info);
        }
        catch (Exception ex)
        {
            UseWaitCursor = false;
            _btnUpdate.Enabled = true;
            _btnUpdate.Text = $"⟳  Actualizar a {info.Tag}";
            MessageBox.Show(
                $"No se pudo completar la actualización.\n\nDetalle: {ex.Message}",
                "Actualización",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void SetupBackupButton()
    {
        if (!_currentUser.IsAdministrator)
        {
            return;
        }

        var btnBackup = new Button
        {
            Text = "Respaldos",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 241, 245),
            BackColor = _sidebar,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Size = new Size(194, 40),
            Location = new Point(18, 404),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        btnBackup.FlatAppearance.BorderSize = 0;
        btnBackup.FlatAppearance.MouseOverBackColor = _sidebarHover;
        btnBackup.FlatAppearance.MouseDownBackColor = _accentDark;
        btnBackup.Click += (_, _) =>
        {
            using var form = new BackupSettingsForm(_currentUser);
            form.ShowDialog(this);
        };
        pnlSidebar.Controls.Add(btnBackup);
    }

    private void SetupBrandLogo()
    {
        if (BrandAssets.Logo is not { } logo)
        {
            return;
        }

        var picLogo = new PictureBox
        {
            Image = logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(42, 42),
            Location = new Point(18, 18),
            BackColor = Color.Transparent
        };
        pnlSidebar.Controls.Add(picLogo);
        picLogo.BringToFront();

        lblBrand.Location = new Point(70, 24);
        lblBrand.Size = new Size(152, 44);
        lblBrand.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UiKit.FadeIn(this);
        CheckForUpdates();
    }

    private void SetupNavigation()
    {
        UiKit.EnableDoubleBuffer(pnlSidebar);

        // Reorganiza los botones del menú en pasos compactos y agrega "Ingresos".
        var y = 110;
        const int step = 46;
        foreach (var button in new[] { btnDashboard, btnParking, btnRates })
        {
            button.Size = new Size(194, 40);
            button.Location = new Point(18, y);
            y += step;
        }

        _btnIncome = new Button
        {
            Text = "Ingresos",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(235, 241, 245),
            BackColor = _sidebar,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Size = new Size(194, 40),
            Location = new Point(18, y),
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            Enabled = _currentUser.HasPermission(PermissionKeys.Income)
        };
        _btnIncome.Click += (_, _) => RunPermissionAction(PermissionKeys.Income, ShowIncome);
        pnlSidebar.Controls.Add(_btnIncome);
        y += step;

        btnUsers.Size = new Size(194, 40);
        btnUsers.Location = new Point(18, y);
        y += step;
        btnClosures.Size = new Size(194, 40);
        btnClosures.Location = new Point(18, y);

        _navIndicator = new Panel
        {
            BackColor = _accent,
            Size = new Size(4, 40),
            Location = new Point(0, btnDashboard.Top)
        };
        pnlSidebar.Controls.Add(_navIndicator);
        _navIndicator.BringToFront();

        _indicatorTimer.Tick += (_, _) =>
        {
            if (_navIndicator is null)
            {
                _indicatorTimer.Stop();
                return;
            }

            var current = _navIndicator.Top;
            var delta = _indicatorTarget - current;
            if (Math.Abs(delta) <= 2)
            {
                _navIndicator.Top = _indicatorTarget;
                _indicatorTimer.Stop();
                return;
            }

            _navIndicator.Top = current + delta / 3;
        };

        foreach (var button in new[] { btnDashboard, btnParking, btnRates, _btnIncome, btnUsers, btnClosures, btnLogout })
        {
            button.BackColor = _sidebar;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = _sidebarHover;
            button.FlatAppearance.MouseDownBackColor = _accentDark;
            button.Padding = new Padding(14, 0, 0, 0);
        }
    }

    private void btnDashboard_Click(object sender, EventArgs e) => ShowDashboard();

    private void btnParking_Click(object sender, EventArgs e) => RunPermissionAction(PermissionKeys.Parking, ShowParking);

    private void btnRates_Click(object sender, EventArgs e) => RunPermissionAction(PermissionKeys.Rates, ShowRates);

    private void btnUsers_Click(object sender, EventArgs e) => RunPermissionAction(PermissionKeys.Users, ShowUsers);

    private void btnClosures_Click(object sender, EventArgs e)
    {
        if (_currentUser.HasPermission(PermissionKeys.EmployeeClosure)
            || _currentUser.HasPermission(PermissionKeys.CashClosure)
            || _currentUser.HasPermission(PermissionKeys.ClosureHistory))
        {
            ShowClosures();
            return;
        }

        MessageBox.Show("No tiene permiso para acceder a este modulo.", "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void btnLogout_Click(object sender, EventArgs e) => Close();

    /// <summary>
    /// Muestra un módulo reutilizando su panel si ya fue creado (conserva lo escrito). El builder
    /// devuelve el panel y una acción de refresco que se ejecuta cada vez que el módulo se muestra.
    /// </summary>
    private void ShowModule(Button button, string title, string key, Func<(Control panel, Action refresh)> builder)
    {
        SetActive(button, title);

        if (!_modulePanels.TryGetValue(key, out var panel))
        {
            pnlContent.SuspendLayout();
            var (created, refresh) = builder();
            created.Dock = DockStyle.Fill;
            _modulePanels[key] = created;
            _moduleRefreshers[key] = refresh;
            pnlContent.Controls.Add(created);
            pnlContent.ResumeLayout();
            panel = created;
        }

        foreach (var pair in _modulePanels)
        {
            pair.Value.Visible = pair.Key == key;
        }

        panel.BringToFront();
        if (_moduleRefreshers.TryGetValue(key, out var refresher))
        {
            refresher();
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Panel
    // ---------------------------------------------------------------------------------------------

    private void ShowDashboard() => ShowModule(btnDashboard, "Panel", "Dashboard", BuildDashboard);

    private (Control, Action) BuildDashboard()
    {
        var container = new Panel { BackColor = pnlContent.BackColor, AutoScroll = true };

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 120,
            ColumnCount = 4,
            RowCount = 1
        };
        for (var i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        var cardActive = CreateStatCard("Vehículos activos", "0", Color.FromArgb(22, 163, 74));
        var cardExits = CreateStatCard("Salidas hoy", "0", _accent);
        var cardCash = CreateStatCard("Efectivo hoy", "₡0", Color.FromArgb(202, 138, 4));
        var cardSinpe = CreateStatCard("SINPE hoy", "₡0", Color.FromArgb(124, 58, 237));
        cards.Controls.Add(cardActive, 0, 0);
        cards.Controls.Add(cardExits, 1, 0);
        cards.Controls.Add(cardCash, 2, 0);
        cards.Controls.Add(cardSinpe, 3, 0);

        var grid = CreateGrid();
        var title = CreateSectionTitle("Vehículos dentro del parqueo");

        container.Controls.Add(grid);
        container.Controls.Add(title);
        container.Controls.Add(cards);

        void Refresh()
        {
            var stats = _controller.GetStats(_currentUser.UserId);
            var today = _controller.GetSummaryForDate(DateTime.Today);
            SetCardValue(cardActive, stats.ActiveVehicles.ToString());
            SetCardValue(cardExits, stats.ExitsToday.ToString());
            SetCardValue(cardCash, MoneyHelper.Format(today.Cash));
            SetCardValue(cardSinpe, MoneyHelper.Format(today.Sinpe));

            grid.DataSource = _controller.GetSessions(activeOnly: true)
                .Select(s => new
                {
                    Placa = s.Plate,
                    Entrada = s.EntryAt.ToString("dd/MM/yyyy HH:mm"),
                    Tarifa = s.HasCustomRate ? "Personalizada" : s.RateName,
                    Tiempo = FormatDuration(s.CurrentDuration)
                })
                .ToList();
        }

        return (container, Refresh);
    }

    // ---------------------------------------------------------------------------------------------
    // Entrada / salida
    // ---------------------------------------------------------------------------------------------

    private void ShowParking() => ShowModule(btnParking, "Entrada / salida", "Parking", BuildParking);

    private (Control, Action) BuildParking()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = pnlContent.BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var left = CreatePanel();
        left.Dock = DockStyle.Fill;
        left.AutoScroll = true;
        var right = CreatePanel();
        right.Dock = DockStyle.Fill;
        right.AutoScroll = true;

        var txtPlate = CreateTextBox();
        txtPlate.CharacterCasing = CharacterCasing.Upper;
        var cmbRates = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11F),
            Width = 300,
            DisplayMember = nameof(ParkingRate.Name),
            ValueMember = nameof(ParkingRate.RateId)
        };
        cmbRates.DataSource = _controller.GetActiveRates();
        var btnEntry = CreatePrimaryButton("Registrar entrada");

        var txtQr = CreateTextBox();
        var btnFindQr = CreateSecondaryButton("Buscar por QR");
        var txtSearchPlate = CreateTextBox();
        txtSearchPlate.CharacterCasing = CharacterCasing.Upper;
        var btnFindPlate = CreateSecondaryButton("Buscar placa");
        var chkHideExited = new CheckBox
        {
            Text = "Ocultar vehiculos con salida",
            Checked = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true
        };

        var lblDetails = CreateInfoLabel("Seleccione un vehículo o escanee un QR.");
        var btnExit = CreatePrimaryButton("Registrar salida");
        btnExit.BackColor = Color.FromArgb(22, 163, 74);
        var btnCustom = CreateSecondaryButton("Tarifa personalizada");
        var btnReprint = CreateSecondaryButton("Reimprimir");

        var grid = CreateGrid(DockStyle.None);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        List<ParkingSession> currentSessions = [];
        ParkingSession? selected = null;

        void LoadGrid(string? plate = null)
        {
            currentSessions = string.IsNullOrWhiteSpace(plate)
                ? _controller.GetSessions(chkHideExited.Checked)
                : _controller.FindByPlate(plate, chkHideExited.Checked);
            grid.DataSource = currentSessions.Select(s => new
            {
                s.SessionId,
                Placa = s.Plate,
                Entrada = s.EntryAt.ToString("dd/MM/yyyy HH:mm"),
                Salida = s.ExitAt?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
                Tarifa = s.HasCustomRate ? "Personalizada" : s.RateName,
                Tiempo = FormatDuration(s.CurrentDuration),
                Monto = s.ChargedAmount?.ToString("C0") ?? string.Empty,
                Pago = s.PaymentMethod ?? string.Empty
            }).ToList();
            HideGridColumn(grid, "SessionId");
        }

        void RenderSelected(ParkingSession? session)
        {
            selected = session;
            if (session is null)
            {
                lblDetails.Text = "Seleccione un vehículo o escanee un QR.";
                return;
            }

            var amount = session.Status == "A"
                ? ParkingService.CalculateAmount(session, DateTime.Now)
                : session.ChargedAmount ?? 0;
            var statusText = session.Status == "A" ? "Activo" : $"Salió: {session.ExitAt:dd/MM/yyyy HH:mm}";
            var rate = session.HasCustomRate ? $"Personalizada ({session.CustomNote ?? "especial"})" : session.RateName;
            lblDetails.Text =
                $"Placa: {session.Plate}\nEntrada: {session.EntryAt:dd/MM/yyyy HH:mm}\nEstado: {statusText}\nTarifa: {rate}\nTiempo: {FormatDuration(session.CurrentDuration)}\nMonto estimado: {MoneyHelper.Format(amount)}";
        }

        btnEntry.Click += (_, _) => ExecuteThrottled(btnEntry, () =>
        {
            if (cmbRates.SelectedValue is not int rateId)
            {
                throw new InvalidOperationException("Seleccione una tarifa.");
            }

            var session = _controller.RegisterEntry(txtPlate.Text, rateId, _currentUser.UserId);
            txtPlate.Clear();
            LoadGrid();
            using var preview = new TicketPreviewForm(session);
            preview.ShowDialog(this);
        });

        btnFindQr.Click += (_, _) => ExecuteWithMessage(() =>
        {
            if (string.IsNullOrWhiteSpace(txtQr.Text))
            {
                return;
            }

            var session = _controller.FindByTicket(txtQr.Text);
            txtQr.Clear();
            txtQr.Focus();
            if (session is null)
            {
                MessageBox.Show("No se encontró un vehículo con ese código QR.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var info = new VehicleInfoForm(session, _controller, _currentUser);
            info.ShowDialog(this);
            if (info.ChangesMade)
            {
                LoadGrid();
                RenderSelected(null);
            }
        });
        txtQr.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnFindQr.PerformClick();
                e.SuppressKeyPress = true;
            }
        };

        btnFindPlate.Click += (_, _) => ExecuteThrottled(btnFindPlate, () =>
        {
            LoadGrid(txtSearchPlate.Text);
            RenderSelected(currentSessions.FirstOrDefault());
        });
        chkHideExited.CheckedChanged += (_, _) =>
        {
            LoadGrid(txtSearchPlate.Text);
            RenderSelected(currentSessions.FirstOrDefault());
        };

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var id = Convert.ToInt64(grid.Rows[e.RowIndex].Cells["SessionId"].Value);
            RenderSelected(currentSessions.FirstOrDefault(s => s.SessionId == id));
        };

        btnReprint.Click += (_, _) => ExecuteThrottled(btnReprint, () =>
        {
            if (selected is null)
            {
                throw new InvalidOperationException("Seleccione o busque un vehículo para reimprimir su tiquete.");
            }

            AuditService.Log(_currentUser.UserId, "ReimprimirTiquete", "ParkingSessions", selected.SessionId.ToString(), $"Placa {selected.Plate}");
            using var preview = new TicketPreviewForm(selected);
            preview.ShowDialog(this);
        });

        btnCustom.Click += (_, _) => ExecuteThrottled(btnCustom, () =>
        {
            if (selected is null || selected.Status != "A")
            {
                throw new InvalidOperationException("Seleccione un vehículo activo para asignarle una tarifa personalizada.");
            }

            using var form = new CustomRateForm(selected);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _controller.SetCustomRate(selected.SessionId, form.RateType, form.Amount, form.GraceMinutes, form.BlockMinutes, form.BlockAmount, form.Note, _currentUser.UserId);
            LoadGrid();
            RenderSelected(_controller.GetSession(selected.SessionId));
            MessageBox.Show("Tarifa personalizada aplicada a esta estadía.", "Tarifa personalizada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        btnExit.Click += (_, _) => ExecuteThrottled(btnExit, () =>
        {
            if (selected is null)
            {
                throw new InvalidOperationException("Seleccione un vehículo para registrar la salida.");
            }

            if (selected.Status != "A")
            {
                throw new InvalidOperationException("Este vehículo ya tiene salida registrada.");
            }

            using var dialog = new ExitPaymentForm(selected);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var closed = _controller.RegisterExit(
                selected.SessionId,
                _currentUser.UserId,
                dialog.ExtraAmount,
                dialog.PaymentMethod,
                dialog.Reference,
                dialog.TenderedAmount);

            var receipt = ExitReceipt.FromClosedSession(closed, dialog.PaymentMethod, dialog.TenderedAmount, dialog.Reference, _currentUser.FullName);
            using var receiptForm = new ReceiptPreviewForm(receipt);
            receiptForm.ShowDialog(this);
            LoadGrid();
            RenderSelected(null);
        });

        AddLabeledControl(left, "Placa del vehículo", txtPlate, 22);
        AddLabeledControl(left, "Tipo de tarifa", cmbRates, 88);
        btnEntry.Location = new Point(24, 156);
        left.Controls.Add(btnEntry);

        AddLabeledControl(left, "Código QR / ticket", txtQr, 224);
        btnFindQr.Location = new Point(24, 294);
        left.Controls.Add(btnFindQr);

        AddLabeledControl(left, "Buscar por placa", txtSearchPlate, 360);
        btnFindPlate.Location = new Point(24, 430);
        left.Controls.Add(btnFindPlate);
        chkHideExited.Location = new Point(24, 488);
        left.Controls.Add(chkHideExited);

        lblDetails.Location = new Point(24, 22);
        lblDetails.Size = new Size(640, 132);
        lblDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        right.Controls.Add(lblDetails);
        btnExit.Location = new Point(24, 162);
        btnExit.Size = new Size(190, 44);
        right.Controls.Add(btnExit);
        btnCustom.Location = new Point(224, 162);
        btnCustom.Size = new Size(190, 44);
        right.Controls.Add(btnCustom);
        btnReprint.Location = new Point(424, 162);
        btnReprint.Size = new Size(150, 44);
        right.Controls.Add(btnReprint);
        grid.Location = new Point(24, 222);
        grid.Size = new Size(640, 414);
        right.Controls.Add(grid);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);

        void Refresh() => LoadGrid(string.IsNullOrWhiteSpace(txtSearchPlate.Text) ? null : txtSearchPlate.Text);

        return (root, Refresh);
    }

    // ---------------------------------------------------------------------------------------------
    // Tarifas
    // ---------------------------------------------------------------------------------------------

    private void ShowRates() => ShowModule(btnRates, "Tarifas", "Rates", BuildRates);

    private (Control, Action) BuildRates()
    {
        var panel = CreatePanel();
        panel.Dock = DockStyle.Fill;
        panel.AutoScroll = true;

        var grid = CreateGrid(DockStyle.None);
        grid.Location = new Point(24, 24);
        grid.Size = new Size(650, 600);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        var txtName = CreateTextBox();
        var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), Width = 260 };
        cmbType.Items.AddRange(["Hora", "Dia", "Semana", "Mes", "Fija"]);
        var txtAmount = CreateMoneyTextBox();
        txtAmount.Width = 260;
        var numGrace = new NumericUpDown { Font = new Font("Segoe UI", 11F), Maximum = 1440, Width = 260 };
        var chkBlock = new CheckBox
        {
            Text = "Tope por bloque de 12h (tarifa por hora que pasa a diaria)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true
        };
        var txtBlock = CreateMoneyTextBox();
        txtBlock.Width = 260;
        txtBlock.Text = "3000";
        var chkActive = new CheckBox { Text = "Activa", Checked = true, Font = new Font("Segoe UI", 10F), Width = 260 };
        var btnSave = CreatePrimaryButton("Guardar tarifa");
        var btnNew = CreateSecondaryButton("Nueva");
        int editingId = 0;

        void ApplyBlockVisibility()
        {
            var isHour = (cmbType.SelectedItem?.ToString() ?? "Hora") == "Hora";
            chkBlock.Visible = isHour;
            txtBlock.Visible = isHour && chkBlock.Checked;
        }

        void LoadRates()
        {
            grid.DataSource = _controller.GetRates().Select(r => new
            {
                r.RateId,
                Nombre = r.Name,
                Tipo = r.RateType,
                Monto = r.Amount,
                Tope12h = r.BlockAmount?.ToString("C0") ?? string.Empty,
                Gracia = r.GraceMinutes,
                Activa = r.IsActive
            }).ToList();
            HideGridColumn(grid, "RateId");
        }

        void ClearForm()
        {
            editingId = 0;
            txtName.Clear();
            cmbType.SelectedIndex = 0;
            txtAmount.Text = "0";
            numGrace.Value = 0;
            chkBlock.Checked = false;
            txtBlock.Text = "3000";
            chkActive.Checked = true;
            ApplyBlockVisibility();
        }

        cmbType.SelectedIndexChanged += (_, _) => ApplyBlockVisibility();
        chkBlock.CheckedChanged += (_, _) => ApplyBlockVisibility();

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var id = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["RateId"].Value);
            var rate = _controller.GetRates().First(r => r.RateId == id);
            editingId = rate.RateId;
            txtName.Text = rate.Name;
            cmbType.SelectedItem = rate.RateType;
            txtAmount.Text = rate.Amount.ToString("0.##");
            numGrace.Value = Math.Min(numGrace.Maximum, rate.GraceMinutes);
            chkBlock.Checked = rate.BlockAmount.HasValue;
            txtBlock.Text = (rate.BlockAmount ?? 3000m).ToString("0.##");
            chkActive.Checked = rate.IsActive;
            ApplyBlockVisibility();
        };

        btnSave.Click += (_, _) => ExecuteThrottled(btnSave, () =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                throw new InvalidOperationException("Digite el nombre de la tarifa.");
            }

            var type = cmbType.SelectedItem?.ToString() ?? "Hora";
            int? blockMinutes = null;
            decimal? blockAmount = null;
            if (type == "Hora" && chkBlock.Checked)
            {
                blockMinutes = 720;
                blockAmount = ParseMoney(txtBlock.Text);
            }

            _controller.SaveRate(new ParkingRate
            {
                RateId = editingId,
                Name = txtName.Text,
                RateType = type,
                Amount = ParseMoney(txtAmount.Text),
                GraceMinutes = Convert.ToInt32(numGrace.Value),
                BlockMinutes = blockMinutes,
                BlockAmount = blockAmount,
                IsActive = chkActive.Checked,
                SortOrder = editingId
            }, _currentUser.UserId);
            LoadRates();
            ClearForm();
        });
        btnNew.Click += (_, _) => ExecuteThrottled(btnNew, ClearForm);

        panel.Controls.Add(grid);
        AddLabeledControl(panel, "Nombre", txtName, 30, 720);
        AddLabeledControl(panel, "Tipo", cmbType, 100, 720);
        AddLabeledControl(panel, "Monto por unidad", txtAmount, 170, 720);
        AddLabeledControl(panel, "Minutos de gracia", numGrace, 240, 720);
        chkBlock.Location = new Point(720, 306);
        panel.Controls.Add(chkBlock);
        AddLabeledControl(panel, "Tope por 12h (máximo a cobrar)", txtBlock, 330, 720);
        chkActive.Location = new Point(720, 406);
        panel.Controls.Add(chkActive);
        btnSave.Location = new Point(720, 446);
        btnNew.Location = new Point(910, 446);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnNew);

        ClearForm();

        return (panel, LoadRates);
    }

    // ---------------------------------------------------------------------------------------------
    // Usuarios
    // ---------------------------------------------------------------------------------------------

    private void ShowUsers() => ShowModule(btnUsers, "Usuarios", "Users", BuildUsers);

    private (Control, Action) BuildUsers()
    {
        var panel = CreatePanel();
        panel.Dock = DockStyle.Fill;
        panel.AutoScroll = true;

        var grid = CreateGrid(DockStyle.None);
        grid.Location = new Point(24, 24);
        grid.Size = new Size(650, 600);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        var txtCedula = CreateTextBox();
        var txtFullName = CreateTextBox();
        var txtPassword = CreateTextBox();
        txtPassword.PasswordChar = '*';
        var cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), Width = 260 };
        cmbRole.Items.AddRange(["Empleado", "Administrador"]);
        var chkActive = new CheckBox { Text = "Activo", Checked = true, Font = new Font("Segoe UI", 10F), Width = 260 };
        var clbPermissions = new CheckedListBox
        {
            CheckOnClick = true,
            Font = new Font("Segoe UI", 10F),
            Width = 320,
            Height = 150,
            BorderStyle = BorderStyle.FixedSingle
        };
        foreach (var permission in PermissionKeys.All)
        {
            clbPermissions.Items.Add(new PermissionListItem(permission, PermissionKeys.Labels[permission]));
        }
        var btnSave = CreatePrimaryButton("Guardar usuario");
        var btnNew = CreateSecondaryButton("Nuevo");
        int? editingId = null;

        void LoadUsers()
        {
            grid.DataSource = _controller.GetUsers().Select(u => new
            {
                u.UserId,
                Cedula = u.IdentificationNumber,
                Nombre = u.FullName,
                Puesto = u.RoleName,
                Activo = u.IsActive
            }).ToList();
            HideGridColumn(grid, "UserId");
        }

        void ClearForm()
        {
            editingId = null;
            txtCedula.Clear();
            txtFullName.Clear();
            txtPassword.Clear();
            cmbRole.SelectedIndex = 0;
            chkActive.Checked = true;
            SetCheckedPermissions([PermissionKeys.Dashboard, PermissionKeys.Parking]);
        }

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            editingId = Convert.ToInt32(grid.Rows[e.RowIndex].Cells["UserId"].Value);
            txtCedula.Text = Convert.ToString(grid.Rows[e.RowIndex].Cells["Cedula"].Value);
            txtFullName.Text = Convert.ToString(grid.Rows[e.RowIndex].Cells["Nombre"].Value);
            cmbRole.SelectedItem = Convert.ToString(grid.Rows[e.RowIndex].Cells["Puesto"].Value);
            chkActive.Checked = Convert.ToBoolean(grid.Rows[e.RowIndex].Cells["Activo"].Value);
            txtPassword.Clear();
            var selectedUser = _controller.GetUsers().First(u => u.UserId == editingId.Value);
            SetCheckedPermissions(selectedUser.IsAdministrator ? PermissionKeys.All : selectedUser.Permissions);
        };

        btnSave.Click += (_, _) => ExecuteThrottled(btnSave, () =>
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text))
            {
                throw new InvalidOperationException("Digite la cedula.");
            }

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                throw new InvalidOperationException("Digite el nombre completo.");
            }

            if (editingId is null && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                throw new InvalidOperationException("Digite una contrasena para el usuario nuevo.");
            }

            _controller.SaveUser(
                editingId,
                txtCedula.Text,
                txtFullName.Text,
                cmbRole.SelectedItem?.ToString() ?? "Empleado",
                string.IsNullOrWhiteSpace(txtPassword.Text) ? null : txtPassword.Text,
                chkActive.Checked,
                GetCheckedPermissions(),
                _currentUser.UserId);
            LoadUsers();
            ClearForm();
        });
        btnNew.Click += (_, _) => ExecuteThrottled(btnNew, ClearForm);
        cmbRole.SelectedIndexChanged += (_, _) =>
        {
            if (cmbRole.SelectedItem?.ToString() == "Administrador")
            {
                SetCheckedPermissions(PermissionKeys.All);
            }
        };

        panel.Controls.Add(grid);
        AddLabeledControl(panel, "Cedula", txtCedula, 24, 720);
        AddLabeledControl(panel, "Nombre completo", txtFullName, 92, 720);
        AddLabeledControl(panel, "Contrasena", txtPassword, 160, 720);
        AddLabeledControl(panel, "Puesto", cmbRole, 228, 720);
        AddLabeledControl(panel, "Permisos", clbPermissions, 296, 720);
        chkActive.Location = new Point(720, 480);
        panel.Controls.Add(chkActive);
        btnSave.Location = new Point(720, 520);
        btnNew.Location = new Point(910, 520);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnNew);

        ClearForm();

        return (panel, LoadUsers);

        void SetCheckedPermissions(IEnumerable<string> permissions)
        {
            var permissionSet = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < clbPermissions.Items.Count; i++)
            {
                var item = (PermissionListItem)clbPermissions.Items[i];
                clbPermissions.SetItemChecked(i, permissionSet.Contains(item.Key));
            }
        }

        List<string> GetCheckedPermissions() =>
            clbPermissions.CheckedItems.Cast<PermissionListItem>().Select(item => item.Key).ToList();
    }

    // ---------------------------------------------------------------------------------------------
    // Ingresos
    // ---------------------------------------------------------------------------------------------

    private void ShowIncome() => ShowModule(_btnIncome!, "Ingresos", "Income", BuildIncome);

    private (Control, Action) BuildIncome()
    {
        var panel = CreatePanel();
        panel.Dock = DockStyle.Fill;
        panel.Padding = new Padding(0);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filters = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 0) };
        var fromDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 140, Value = DateTime.Today };
        var toDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 140, Value = DateTime.Today };
        var cmbMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Width = 140 };
        cmbMethod.Items.AddRange(["Todos", PaymentMethods.Cash, PaymentMethods.Sinpe]);
        cmbMethod.SelectedIndex = 0;
        var cmbUser = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Width = 180,
            DisplayMember = nameof(User.FullName),
            ValueMember = nameof(User.UserId)
        };
        var userOptions = new List<User> { new() { UserId = 0, FullName = "Todos" } };
        userOptions.AddRange(_controller.GetUsers());
        cmbUser.DataSource = userOptions;
        var btnSearch = CreateSecondaryButton("Buscar");
        var btnPdf = CreatePrimaryButton("Descargar PDF del rango");
        btnPdf.Width = 240;

        AddLabeledControl(filters, "Desde", fromDate, 4, 0);
        AddLabeledControl(filters, "Hasta", toDate, 4, 160);
        AddLabeledControl(filters, "Forma de pago", cmbMethod, 4, 320);
        AddLabeledControl(filters, "Empleado", cmbUser, 4, 480);
        btnSearch.Location = new Point(680, 30);
        btnSearch.Size = new Size(120, 40);
        filters.Controls.Add(btnSearch);
        btnPdf.Location = new Point(812, 30);
        btnPdf.Size = new Size(240, 40);
        filters.Controls.Add(btnPdf);

        var totals = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 8) };
        var lblTotals = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Efectivo: ₡0     SINPE: ₡0     Total: ₡0     Cobros: 0"
        };
        totals.Controls.Add(lblTotals);

        var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 24, 24) };
        var grid = CreateGrid(DockStyle.Fill);
        gridHost.Controls.Add(grid);

        List<IncomeRecord> current = [];

        (DateTime from, DateTime to, string? method, int? userId) ReadFilters()
        {
            var from = fromDate.Value.Date;
            var to = toDate.Value.Date.AddDays(1).AddSeconds(-1);
            var method = cmbMethod.SelectedIndex <= 0 ? null : cmbMethod.SelectedItem?.ToString();
            var userId = cmbUser.SelectedValue is int id && id > 0 ? id : (int?)null;
            return (from, to, method, userId);
        }

        void Load()
        {
            var (from, to, method, userId) = ReadFilters();
            current = _controller.GetIncome(from, to, method, userId);
            grid.DataSource = current.Select(r => new
            {
                Fecha = r.PaidAt.ToString("dd/MM/yyyy HH:mm"),
                Placa = r.Plate,
                Tarifa = r.IsCustom ? "Personalizada" : r.RateName,
                Pago = r.PaymentMethod,
                Monto = r.Amount,
                Referencia = r.Reference ?? string.Empty,
                Empleado = r.Username
            }).ToList();

            var summary = _controller.GetIncomeSummary(from, to, userId);
            lblTotals.Text = $"Efectivo: {MoneyHelper.Format(summary.Cash)}     SINPE: {MoneyHelper.Format(summary.Sinpe)}     Total: {MoneyHelper.Format(summary.Total)}     Cobros: {summary.Count}";
        }

        btnSearch.Click += (_, _) => ExecuteThrottled(btnSearch, Load);
        btnPdf.Click += (_, _) => ExecuteThrottled(btnPdf, () =>
        {
            var (from, to, _, userId) = ReadFilters();
            if (current.Count == 0)
            {
                throw new InvalidOperationException("No hay ingresos para exportar en el rango seleccionado.");
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Ingresos_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var summary = _controller.GetIncomeSummary(from, to, userId);
                _pdfExportService.ExportIncomeRange(current, summary, from, to, dialog.FileName);
                MessageBox.Show("PDF de ingresos generado correctamente.", "Ingresos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        });

        layout.Controls.Add(filters, 0, 0);
        layout.Controls.Add(totals, 0, 1);
        layout.Controls.Add(gridHost, 0, 2);
        panel.Controls.Add(layout);

        return (panel, Load);
    }

    // ---------------------------------------------------------------------------------------------
    // Cierres
    // ---------------------------------------------------------------------------------------------

    private void ShowClosures() => ShowModule(btnClosures, "Cierres", "Closures", BuildClosures);

    private (Control, Action) BuildClosures()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        var tabRegister = new TabPage("Registrar cierres") { BackColor = pnlContent.BackColor, Padding = new Padding(0) };
        var tabHistory = new TabPage("Historial") { BackColor = pnlContent.BackColor, Padding = new Padding(0) };
        tabs.TabPages.Add(tabRegister);
        tabs.TabPages.Add(tabHistory);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var employeePanel = CreatePanel();
        var cashPanel = CreatePanel();
        employeePanel.Dock = DockStyle.Fill;
        cashPanel.Dock = DockStyle.Fill;
        employeePanel.AutoScroll = true;
        cashPanel.AutoScroll = true;

        Action refreshEmployee = BuildEmployeeClosure(employeePanel);
        Action refreshCash = BuildCashClosure(cashPanel);

        root.Controls.Add(employeePanel, 0, 0);
        root.Controls.Add(cashPanel, 1, 0);
        tabRegister.Controls.Add(root);
        BuildClosureHistoryTab(tabHistory);
        tabHistory.Enabled = _currentUser.HasPermission(PermissionKeys.ClosureHistory);

        return (tabs, () =>
        {
            refreshEmployee();
            refreshCash();
        });
    }

    private Action BuildEmployeeClosure(Panel employeePanel)
    {
        var cmbUsers = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11F),
            Width = 300,
            DisplayMember = nameof(User.FullName),
            ValueMember = nameof(User.UserId),
            DataSource = _currentUser.IsAdministrator ? _controller.GetUsers() : new List<User> { _currentUser }
        };
        var fromPicker = new DateTimePicker { Font = new Font("Segoe UI", 10F), Width = 300, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = DateTime.Today };
        var toPicker = new DateTimePicker { Font = new Font("Segoe UI", 10F), Width = 300, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = DateTime.Now };

        var lblExpectedCash = CreateInfoLabel("Efectivo esperado: ₡0");
        var lblExpectedSinpe = CreateInfoLabel("SINPE cobrado: ₡0");
        var lblCounted = CreateInfoLabel("Entregado (contado): ₡0");
        var lblDiff = CreateInfoLabel("Diferencia: ₡0");
        foreach (var lbl in new[] { lblExpectedCash, lblExpectedSinpe, lblCounted, lblDiff })
        {
            lbl.Size = new Size(440, 22);
        }

        var (denomPanel, denomInputs) = CreateDenominationPanel();
        var btnExpected = CreateSecondaryButton("Calcular esperado");
        var btnEmployeeClose = CreatePrimaryButton("Cerrar empleado");

        decimal cashExpected = 0m;

        void UpdateSummary()
        {
            var counted = denomInputs.Sum(item => item.Key * item.Value.Value);
            lblCounted.Text = $"Entregado (contado): {MoneyHelper.Format(counted)}";
            var difference = counted - cashExpected;
            var estado = difference == 0 ? "✔ Cuadra" : difference > 0 ? "▲ Sobra" : "▼ Falta";
            lblDiff.Text = $"Diferencia: {MoneyHelper.Format(difference)}    {estado}";
            lblDiff.ForeColor = difference == 0
                ? Color.FromArgb(22, 163, 74)
                : difference > 0 ? Color.FromArgb(202, 138, 4) : Color.FromArgb(220, 38, 38);
        }

        void CalculateExpected()
        {
            if (cmbUsers.SelectedValue is not int userId)
            {
                return;
            }

            var totals = _controller.GetUserTotals(userId, fromPicker.Value, toPicker.Value);
            cashExpected = totals.Cash;
            lblExpectedCash.Text = $"Efectivo esperado: {MoneyHelper.Format(totals.Cash)}";
            lblExpectedSinpe.Text = $"SINPE cobrado: {MoneyHelper.Format(totals.Sinpe)}";
            UpdateSummary();
        }

        foreach (var input in denomInputs.Values)
        {
            input.ValueChanged += (_, _) => UpdateSummary();
        }

        btnExpected.Click += (_, _) => ExecuteThrottled(btnExpected, CalculateExpected);

        btnEmployeeClose.Click += (_, _) => ExecuteThrottled(btnEmployeeClose, () =>
        {
            if (cmbUsers.SelectedValue is not int userId)
            {
                throw new InvalidOperationException("Seleccione un empleado.");
            }

            var denominations = denomInputs.ToDictionary(item => item.Key, item => Convert.ToInt32(item.Value.Value));
            var closure = _controller.CreateEmployeeClosure(userId, fromPicker.Value, toPicker.Value, denominations, _currentUser.UserId);
            MessageBox.Show(
                $"Cierre de empleado #{closure.ClosureId} creado.\n\n"
                + $"Efectivo esperado: {MoneyHelper.Format(closure.CashExpected)}\n"
                + $"SINPE cobrado:     {MoneyHelper.Format(closure.SinpeExpected)}\n"
                + $"Entregado:         {MoneyHelper.Format(closure.DeliveredAmount)}\n"
                + $"Diferencia:        {MoneyHelper.Format(closure.DifferenceAmount)}",
                "Cierre de empleado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            foreach (var input in denomInputs.Values)
            {
                input.Value = 0;
            }
        });

        employeePanel.Controls.Add(CreateSectionTitle("Cierre de empleado", 24, 16));
        AddLabeledControl(employeePanel, "Empleado", cmbUsers, 56);
        AddLabeledControl(employeePanel, "Desde", fromPicker, 116);
        AddLabeledControl(employeePanel, "Hasta", toPicker, 176);
        lblExpectedCash.Location = new Point(24, 234);
        lblExpectedSinpe.Location = new Point(24, 258);
        employeePanel.Controls.Add(lblExpectedCash);
        employeePanel.Controls.Add(lblExpectedSinpe);
        employeePanel.Controls.Add(CreateSectionTitle("Billetes y monedas entregados", 24, 288));
        denomPanel.Location = new Point(24, 318);
        employeePanel.Controls.Add(denomPanel);
        lblCounted.Location = new Point(24, 318 + denomPanel.Height + 6);
        lblDiff.Location = new Point(24, 318 + denomPanel.Height + 30);
        employeePanel.Controls.Add(lblCounted);
        employeePanel.Controls.Add(lblDiff);
        btnExpected.Location = new Point(24, 318 + denomPanel.Height + 60);
        btnEmployeeClose.Location = new Point(220, 318 + denomPanel.Height + 60);
        employeePanel.Controls.Add(btnExpected);
        employeePanel.Controls.Add(btnEmployeeClose);

        var hasPermission = _currentUser.HasPermission(PermissionKeys.EmployeeClosure);
        if (!hasPermission)
        {
            cmbUsers.Enabled = false;
            fromPicker.Enabled = false;
            toPicker.Enabled = false;
            denomPanel.Enabled = false;
            btnExpected.Enabled = false;
            btnEmployeeClose.Enabled = false;
            lblExpectedCash.Text = "El cierre de empleado requiere permiso asignado.";
        }

        return () =>
        {
            if (hasPermission)
            {
                CalculateExpected();
            }
        };
    }

    private Action BuildCashClosure(Panel cashPanel)
    {
        var lblCashDate = CreateInfoLabel(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lblCashDate.Size = new Size(300, 24);
        var baseAmount = AppSettings.MinimumCashAmount;

        var lblBase = CreateInfoLabel($"Fondo de caja (base): {MoneyHelper.Format(baseAmount)}");
        lblBase.ForeColor = Color.FromArgb(100, 116, 139);
        var lblCash = CreateInfoLabel("Efectivo cobrado hoy: ₡0");
        var lblSinpe = CreateInfoLabel("SINPE cobrado hoy: ₡0");
        var lblExpectedCash = CreateInfoLabel("Esperado en caja (efectivo + fondo): ₡0");
        var lblCounted = CreateInfoLabel("Contado (físico): ₡0");
        var lblDiff = CreateInfoLabel("Diferencia: ₡0");
        foreach (var lbl in new[] { lblBase, lblCash, lblSinpe, lblExpectedCash, lblCounted, lblDiff })
        {
            lbl.Size = new Size(450, 22);
        }

        var (denomPanel, denomInputs) = CreateDenominationPanel();
        var btnSystem = CreateSecondaryButton("Recalcular");
        var btnCashClose = CreatePrimaryButton("Cerrar caja");

        decimal cashSystem = 0m;
        decimal sinpeSystem = 0m;

        void UpdateSummary()
        {
            var counted = denomInputs.Sum(item => item.Key * item.Value.Value);
            var expected = cashSystem + baseAmount;
            var difference = counted - expected;
            lblCash.Text = $"Efectivo cobrado hoy: {MoneyHelper.Format(cashSystem)}";
            lblSinpe.Text = $"SINPE cobrado hoy: {MoneyHelper.Format(sinpeSystem)}";
            lblExpectedCash.Text = $"Esperado en caja (efectivo + fondo): {MoneyHelper.Format(expected)}";
            lblCounted.Text = $"Contado (físico): {MoneyHelper.Format(counted)}";
            var estado = difference == 0 ? "✔ Cuadra" : difference > 0 ? "▲ Sobra" : "▼ Falta";
            lblDiff.Text = $"Diferencia: {MoneyHelper.Format(difference)}    {estado}";
            lblDiff.ForeColor = difference == 0
                ? Color.FromArgb(22, 163, 74)
                : difference > 0 ? Color.FromArgb(202, 138, 4) : Color.FromArgb(220, 38, 38);
        }

        void Recalculate()
        {
            var totals = _controller.GetSummaryForDate(DateTime.Today);
            cashSystem = totals.Cash;
            sinpeSystem = totals.Sinpe;
            lblCashDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            UpdateSummary();
        }

        foreach (var input in denomInputs.Values)
        {
            input.ValueChanged += (_, _) => UpdateSummary();
        }

        btnSystem.Click += (_, _) => ExecuteThrottled(btnSystem, Recalculate);

        btnCashClose.Click += (_, _) => ExecuteThrottled(btnCashClose, () =>
        {
            var denominations = denomInputs.ToDictionary(item => item.Key, item => Convert.ToInt32(item.Value.Value));
            var counted = denominations.Sum(item => item.Key * item.Value);
            var expected = cashSystem + baseAmount;
            var difference = counted - expected;
            var closureId = _controller.CreateCashClosure(DateTime.Now, denominations, _currentUser.UserId);
            MessageBox.Show(
                $"Cierre de caja #{closureId} creado.\n\n"
                + $"Fondo base:       {MoneyHelper.Format(baseAmount)}\n"
                + $"Efectivo sistema: {MoneyHelper.Format(cashSystem)}\n"
                + $"SINPE sistema:    {MoneyHelper.Format(sinpeSystem)}\n"
                + $"Esperado caja:    {MoneyHelper.Format(expected)}\n"
                + $"Contado:          {MoneyHelper.Format(counted)}\n"
                + $"Diferencia:       {MoneyHelper.Format(difference)}",
                "Cierre de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            foreach (var input in denomInputs.Values)
            {
                input.Value = 0;
            }
            Recalculate();
        });

        cashPanel.Controls.Add(CreateSectionTitle("Cierre de caja", 24, 16));
        AddLabeledControl(cashPanel, "Fecha automática", lblCashDate, 56);
        lblBase.Location = new Point(24, 112);
        lblCash.Location = new Point(24, 134);
        lblSinpe.Location = new Point(24, 156);
        lblExpectedCash.Location = new Point(24, 178);
        cashPanel.Controls.Add(lblBase);
        cashPanel.Controls.Add(lblCash);
        cashPanel.Controls.Add(lblSinpe);
        cashPanel.Controls.Add(lblExpectedCash);
        cashPanel.Controls.Add(CreateSectionTitle("Billetes y monedas", 24, 208));
        denomPanel.Location = new Point(24, 238);
        cashPanel.Controls.Add(denomPanel);
        lblCounted.Location = new Point(24, 238 + denomPanel.Height + 6);
        lblDiff.Location = new Point(24, 238 + denomPanel.Height + 30);
        cashPanel.Controls.Add(lblCounted);
        cashPanel.Controls.Add(lblDiff);
        btnSystem.Location = new Point(24, 238 + denomPanel.Height + 60);
        btnCashClose.Location = new Point(220, 238 + denomPanel.Height + 60);
        cashPanel.Controls.Add(btnSystem);
        cashPanel.Controls.Add(btnCashClose);

        var hasPermission = _currentUser.HasPermission(PermissionKeys.CashClosure);
        if (!hasPermission)
        {
            btnCashClose.Enabled = false;
            denomPanel.Enabled = false;
            btnSystem.Enabled = false;
            lblCash.Visible = false;
            lblSinpe.Visible = false;
            lblExpectedCash.Visible = false;
            lblCounted.Visible = false;
            lblDiff.Visible = false;
            lblBase.Text = "El cierre de caja requiere permiso asignado.";
        }

        return () =>
        {
            if (hasPermission)
            {
                Recalculate();
            }
        };
    }

    private (Panel panel, Dictionary<decimal, NumericUpDown> inputs) CreateDenominationPanel()
    {
        var inputs = new Dictionary<decimal, NumericUpDown>();
        var flow = new FlowLayoutPanel
        {
            Size = new Size(456, 264),
            AutoScroll = false,
            WrapContents = true
        };

        foreach (var denomination in DenominationValues)
        {
            var row = new Panel { Width = 214, Height = 34, Margin = new Padding(0, 0, 8, 8) };
            var label = new Label
            {
                Text = MoneyHelper.Format(denomination),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 7),
                Width = 80
            };
            var input = new NumericUpDown
            {
                Font = new Font("Segoe UI", 9F),
                Width = 110,
                Maximum = 100000,
                Location = new Point(88, 3)
            };
            inputs.Add(denomination, input);
            row.Controls.Add(label);
            row.Controls.Add(input);
            flow.Controls.Add(row);
        }

        return (flow, inputs);
    }

    private void BuildClosureHistoryTab(TabPage tabHistory)
    {
        if (!_currentUser.HasPermission(PermissionKeys.ClosureHistory))
        {
            var panel = CreatePanel();
            panel.Dock = DockStyle.Fill;
            var label = CreateInfoLabel("El historial de cierres requiere permiso asignado.");
            label.Location = new Point(28, 28);
            label.Size = new Size(520, 40);
            panel.Controls.Add(label);
            tabHistory.Controls.Add(panel);
            return;
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var listPanel = CreatePanel();
        var previewPanel = CreatePanel();
        listPanel.Dock = DockStyle.Fill;
        previewPanel.Dock = DockStyle.Fill;
        listPanel.Padding = new Padding(24);
        previewPanel.Padding = new Padding(24);

        var cmbType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F),
            Width = 140
        };
        cmbType.Items.AddRange(["Todos", "Empleado", "Caja"]);
        cmbType.SelectedIndex = 0;

        var fromDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today.AddDays(-7) };
        var toDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 120, Value = DateTime.Today };
        var btnSearch = CreateSecondaryButton("Buscar");
        var grid = CreateGrid(DockStyle.Fill);

        var preview = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10F),
            ReadOnly = true,
            BackColor = Color.White
        };
        var btnPdf = CreatePrimaryButton("PDF del cierre");
        var btnPdfRange = CreateSecondaryButton("PDF del rango");

        List<ClosureHistoryRecord> currentRecords = [];
        ClosureHistoryRecord? selectedRecord = null;

        void LoadHistory()
        {
            currentRecords.Clear();
            var selectedType = cmbType.SelectedItem?.ToString() ?? "Todos";
            if (selectedType is "Todos" or "Empleado")
            {
                currentRecords.AddRange(_controller.GetEmployeeClosureHistory(fromDate.Value, toDate.Value));
            }

            if (selectedType is "Todos" or "Caja")
            {
                currentRecords.AddRange(_controller.GetCashClosureHistory(fromDate.Value, toDate.Value));
            }

            currentRecords = currentRecords.OrderByDescending(r => r.CreatedAt).ToList();
            grid.DataSource = currentRecords.Select(r => new
            {
                Tipo = r.ClosureType,
                Id = r.ClosureId,
                Fecha = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Nombre = r.ClosureType == "Caja" ? r.CreatedByName : r.EmployeeName,
                Total = r.ClosureType == "Caja" ? r.CountedAmount : r.DeliveredAmount,
                Diferencia = r.DifferenceAmount
            }).ToList();
            HideGridColumn(grid, "Id");
            selectedRecord = currentRecords.FirstOrDefault();
            preview.Text = selectedRecord is null ? "No hay cierres para mostrar." : BuildClosurePreview(selectedRecord);
        }

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var type = Convert.ToString(grid.Rows[e.RowIndex].Cells["Tipo"].Value) ?? string.Empty;
            var id = Convert.ToInt64(grid.Rows[e.RowIndex].Cells["Id"].Value);
            selectedRecord = currentRecords.FirstOrDefault(r => r.ClosureType == type && r.ClosureId == id);
            preview.Text = selectedRecord is null ? string.Empty : BuildClosurePreview(selectedRecord);
        };

        btnSearch.Click += (_, _) => ExecuteThrottled(btnSearch, LoadHistory);
        btnPdf.Click += (_, _) => ExecuteThrottled(btnPdf, () =>
        {
            if (selectedRecord is null)
            {
                throw new InvalidOperationException("Seleccione un cierre para exportar.");
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = _pdfExportService.BuildDefaultFileName(selectedRecord)
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pdfExportService.ExportClosure(selectedRecord, dialog.FileName);
                MessageBox.Show("PDF generado correctamente.", "Historial de cierres", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        });
        btnPdfRange.Click += (_, _) => ExecuteThrottled(btnPdfRange, () =>
        {
            if (currentRecords.Count == 0)
            {
                throw new InvalidOperationException("No hay cierres en el rango para exportar.");
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = $"Cierres_{fromDate.Value:yyyyMMdd}_{toDate.Value:yyyyMMdd}.pdf"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pdfExportService.ExportClosureRange(currentRecords, fromDate.Value, toDate.Value, dialog.FileName);
                MessageBox.Show("PDF del rango generado correctamente.", "Historial de cierres", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        });

        var filterPanel = new Panel { Dock = DockStyle.Fill };
        AddLabeledControl(filterPanel, "Tipo", cmbType, 4, 0);
        AddLabeledControl(filterPanel, "Desde", fromDate, 4, 160);
        AddLabeledControl(filterPanel, "Hasta", toDate, 4, 300);
        btnSearch.Location = new Point(0, 62);
        btnSearch.Size = new Size(120, 34);
        filterPanel.Controls.Add(btnSearch);

        var listLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        listLayout.Controls.Add(filterPanel, 0, 0);
        listLayout.Controls.Add(grid, 0, 1);
        listPanel.Controls.Add(listLayout);

        var buttonRow = new Panel { Dock = DockStyle.Fill };
        btnPdf.Location = new Point(0, 8);
        btnPdf.Size = new Size(180, 42);
        btnPdfRange.Location = new Point(190, 8);
        btnPdfRange.Size = new Size(180, 42);
        buttonRow.Controls.Add(btnPdf);
        buttonRow.Controls.Add(btnPdfRange);

        var previewLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        previewLayout.Controls.Add(CreateSectionTitle("Vista previa"), 0, 0);
        previewLayout.Controls.Add(preview, 0, 1);
        previewLayout.Controls.Add(buttonRow, 0, 2);
        previewPanel.Controls.Add(previewLayout);

        root.Controls.Add(listPanel, 0, 0);
        root.Controls.Add(previewPanel, 1, 0);
        tabHistory.Controls.Add(root);
        LoadHistory();
    }

    private void RunPermissionAction(string permissionKey, Action action)
    {
        if (!_currentUser.HasPermission(permissionKey))
        {
            MessageBox.Show("No tiene permiso para acceder a este modulo.", "Permisos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        action();
    }

    private void ExecuteWithMessage(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Parqueo Mahischa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AuditService.Log(_currentUser.UserId, "Error", "Aplicacion", null, $"{ex.GetType().Name}: {ex.Message}");
            MessageBox.Show(
                "Ocurrió un error inesperado. Intente de nuevo; si el problema continúa, contacte al administrador.",
                "Parqueo Mahischa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExecuteThrottled(Button button, Action action)
    {
        if (!button.Enabled)
        {
            return;
        }

        button.Enabled = false;
        try
        {
            ExecuteWithMessage(action);
        }
        finally
        {
            var timer = new System.Windows.Forms.Timer { Interval = 2500 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!button.IsDisposed)
                {
                    button.Enabled = true;
                }
            };
            timer.Start();
        }
    }

    private static string BuildClosurePreview(ClosureHistoryRecord record)
    {
        var lines = new List<string>
        {
            record.DisplayName,
            new string('-', 42),
            $"Generado: {record.CreatedAt:dd/MM/yyyy HH:mm}",
            $"Realizado por: {record.CreatedByName}",
            string.Empty
        };

        if (record.ClosureType == "Caja")
        {
            lines.AddRange(
            [
                $"Efectivo (sistema): {MoneyHelper.Format(record.CashAmount)}",
                $"SINPE (sistema):    {MoneyHelper.Format(record.SinpeAmount)}",
                $"Fondo de caja:      {MoneyHelper.Format(record.MinimumCashAmount)}",
                $"Esperado en caja:   {MoneyHelper.Format(record.CashAmount + record.MinimumCashAmount)}",
                $"Contado fisico:     {MoneyHelper.Format(record.CountedAmount)}",
                $"Diferencia:         {MoneyHelper.Format(record.DifferenceAmount)}",
                string.Empty,
                "Billetes y monedas:"
            ]);

            lines.AddRange(record.Denominations.Select(d =>
                $"{MoneyHelper.Format(d.Denomination),10} x {d.Quantity,4} = {MoneyHelper.Format(d.TotalAmount),12}"));
        }
        else
        {
            lines.AddRange(
            [
                $"Empleado:   {record.EmployeeName}",
                $"Desde:      {record.FromAt:dd/MM/yyyy HH:mm}",
                $"Hasta:      {record.ToAt:dd/MM/yyyy HH:mm}",
                $"Efectivo:   {MoneyHelper.Format(record.CashAmount)}",
                $"SINPE:      {MoneyHelper.Format(record.SinpeAmount)}",
                $"Esperado:   {MoneyHelper.Format(record.ExpectedAmount)}",
                $"Entregado:  {MoneyHelper.Format(record.DeliveredAmount)}",
                $"Diferencia: {MoneyHelper.Format(record.DifferenceAmount)}"
            ]);

            if (record.Denominations.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Billetes y monedas entregados:");
                lines.AddRange(record.Denominations.Select(d =>
                    $"{MoneyHelper.Format(d.Denomination),10} x {d.Quantity,4} = {MoneyHelper.Format(d.TotalAmount),12}"));
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void SetActive(Button button, string title)
    {
        lblTitle.Text = title;
        _currentHelpKey = title;
        if (_activeMenuButton is not null)
        {
            _activeMenuButton.BackColor = _sidebar;
            _activeMenuButton.FlatAppearance.MouseOverBackColor = _sidebarHover;
        }

        _activeMenuButton = button;
        button.BackColor = _accentDark;
        button.FlatAppearance.MouseOverBackColor = _accentDark;

        if (_navIndicator is not null)
        {
            _indicatorTarget = button.Top;
            if (!_indicatorTimer.Enabled)
            {
                _indicatorTimer.Start();
            }
        }
    }

    private Panel CreatePanel() => new()
    {
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 0, 18, 0),
        Padding = new Padding(18)
    };

    private Panel CreateStatCard(string title, string value, Color color)
    {
        var panel = CreatePanel();
        panel.Margin = new Padding(0, 0, 16, 0);
        var accentBar = new Panel
        {
            BackColor = color,
            Dock = DockStyle.Left,
            Width = 5
        };
        var lblValue = new Label
        {
            Name = "value",
            Text = value,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = color,
            Location = new Point(18, 18),
            AutoSize = true
        };
        var lblTitleCard = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = _muted,
            Location = new Point(20, 70),
            AutoSize = true
        };
        panel.Controls.Add(lblValue);
        panel.Controls.Add(lblTitleCard);
        panel.Controls.Add(accentBar);
        return panel;
    }

    private static void SetCardValue(Panel card, string value)
    {
        if (card.Controls["value"] is Label label)
        {
            label.Text = value;
        }
    }

    private Label CreateSectionTitle(string text) => CreateSectionTitle(text, 0, 0);

    private Label CreateSectionTitle(string text, int x, int y) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 14F, FontStyle.Bold),
        ForeColor = Color.FromArgb(15, 23, 42),
        AutoSize = true,
        Dock = x == 0 && y == 0 ? DockStyle.Top : DockStyle.None,
        Location = new Point(x, y),
        Height = 42,
        Padding = new Padding(0, 0, 0, 12)
    };

    private TextBox CreateTextBox() => new()
    {
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 12F),
        Width = 300
    };

    private TextBox CreateMoneyTextBox()
    {
        var textBox = CreateTextBox();
        textBox.Width = 300;
        textBox.Text = "0";
        textBox.TextAlign = HorizontalAlignment.Right;
        return textBox;
    }

    private static decimal ParseMoney(string value)
    {
        if (decimal.TryParse(value, out var amount) && amount >= 0)
        {
            return amount;
        }

        throw new InvalidOperationException("Digite un monto valido.");
    }

    private Label CreateInfoLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(30, 41, 59),
        AutoSize = false
    };

    private Button CreatePrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            BackColor = _accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(178, 44),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = _accent;
        UiKit.RoundCorners(button, 10);
        UiKit.AttachHover(button, _accent, _accentDark);
        return button;
    }

    private Button CreateSecondaryButton(string text)
    {
        var rest = Color.FromArgb(246, 249, 251);
        var button = new Button
        {
            Text = text,
            BackColor = rest,
            ForeColor = Color.FromArgb(40, 52, 65),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Size = new Size(178, 44),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(202, 213, 224);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = rest;
        UiKit.RoundCorners(button, 10);
        UiKit.AttachHover(button, rest, Color.FromArgb(233, 239, 244));
        return button;
    }

    private DataGridView CreateGrid(DockStyle dock = DockStyle.Fill)
    {
        var grid = new DataGridView
        {
            Dock = dock,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            Font = new Font("Segoe UI", 10F)
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grid.EnableHeadersVisualStyles = false;
        return grid;
    }

    private static void AddLabeledControl(Control parent, string label, Control control, int y, int x = 24)
    {
        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoSize = true,
            Location = new Point(x, y)
        };
        control.Location = new Point(x, y + 26);
        parent.Controls.Add(lbl);
        parent.Controls.Add(control);
    }

    private static void HideGridColumn(DataGridView grid, string columnName)
    {
        if (grid.Columns.Contains(columnName))
        {
            var column = grid.Columns[columnName];
            if (column is not null)
            {
                column.Visible = false;
            }
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
        }

        return $"{(int)duration.TotalHours}h {duration.Minutes}m";
    }
}

internal sealed class PermissionListItem(string key, string label)
{
    public string Key { get; } = key;

    public override string ToString() => label;
}
