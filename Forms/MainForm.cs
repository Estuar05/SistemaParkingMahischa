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
    private Panel? _navIndicator;
    private readonly System.Windows.Forms.Timer _indicatorTimer = new() { Interval = 12 };
    private int _indicatorTarget;
    private Button? _activeMenuButton;
    private Button? _btnUpdate;
    private string _currentHelpKey = "Panel";

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
            Size = new Size(194, 42),
            Location = new Point(18, 386),
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
            Size = new Size(46, 46),
            Location = new Point(18, 20),
            BackColor = Color.Transparent
        };
        pnlSidebar.Controls.Add(picLogo);
        picLogo.BringToFront();

        lblBrand.Location = new Point(74, 28);
        lblBrand.Size = new Size(148, 50);
        lblBrand.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
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

        _navIndicator = new Panel
        {
            BackColor = _accent,
            Size = new Size(4, 42),
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

            // Movimiento con desaceleración suave hacia el objetivo.
            _navIndicator.Top = current + delta / 3;
        };

        foreach (var button in new[] { btnDashboard, btnParking, btnRates, btnUsers, btnClosures, btnLogout })
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

    private void btnParking_Click(object sender, EventArgs e) => ShowParking();

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

    private void ShowDashboard()
    {
        SetActive(btnDashboard, "Panel");
        ClearContent();

        var stats = _controller.GetStats(_currentUser.UserId);
        var grid = CreateGrid();
        var active = _controller.GetSessions(activeOnly: true)
            .Select(s => new
            {
                Placa = s.Plate,
                Entrada = s.EntryAt.ToString("dd/MM/yyyy HH:mm"),
                Tarifa = s.RateName,
                Tiempo = FormatDuration(s.CurrentDuration)
            })
            .ToList();

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 128,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 22)
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(CreateStatCard("Vehículos activos", stats.ActiveVehicles.ToString(), Color.FromArgb(22, 163, 74)), 0, 0);
        cards.Controls.Add(CreateStatCard("Salidas hoy", stats.ExitsToday.ToString(), _accent), 1, 0);
        cards.Controls.Add(CreateStatCard("Caja del día", MoneyHelper.Format(stats.RevenueToday), Color.FromArgb(202, 138, 4)), 2, 0);
        cards.Controls.Add(CreateStatCard("Cobrado por mí", MoneyHelper.Format(stats.RevenueCurrentUserToday), Color.FromArgb(124, 58, 237)), 3, 0);

        grid.DataSource = active;

        pnlContent.Controls.Add(grid);
        pnlContent.Controls.Add(CreateSectionTitle("Vehículos dentro del parqueo"));
        pnlContent.Controls.Add(cards);
    }

    private void ShowParking()
    {
        SetActive(btnParking, "Entrada / salida");
        ClearContent();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = pnlContent.BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var left = CreatePanel();
        left.Dock = DockStyle.Fill;
        var right = CreatePanel();
        right.Dock = DockStyle.Fill;

        var txtPlate = CreateTextBox();
        txtPlate.CharacterCasing = CharacterCasing.Upper;
        var cmbRates = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11F),
            Width = 320,
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
        var btnReprint = CreateSecondaryButton("Reimprimir tiquete");

        var grid = CreateGrid(DockStyle.None);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        List<ParkingSession> currentSessions = new();
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
                Tarifa = s.RateName,
                Tiempo = FormatDuration(s.CurrentDuration),
                Monto = s.ChargedAmount?.ToString("C0") ?? string.Empty
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
                ? ParkingService.CalculateAmount(session.EntryAt, DateTime.Now, session.RateType, session.RateAmount, session.GraceMinutes)
                : session.ChargedAmount ?? 0;
            var statusText = session.Status == "A" ? "Activo" : $"Salio: {session.ExitAt:dd/MM/yyyy HH:mm}";
            lblDetails.Text =
                $"Placa: {session.Plate}\nEntrada: {session.EntryAt:dd/MM/yyyy HH:mm}\nEstado: {statusText}\nTarifa: {session.RateName}\nTiempo: {FormatDuration(session.CurrentDuration)}\nMonto: {MoneyHelper.Format(amount)}";
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

        btnExit.Click += (_, _) => ExecuteThrottled(btnExit, () =>
        {
            if (selected is null)
            {
                throw new InvalidOperationException("Seleccione un vehículo para registrar la salida.");
            }

            var preview = selected.Status == "A"
                ? ParkingService.CalculateAmount(selected.EntryAt, DateTime.Now, selected.RateType, selected.RateAmount, selected.GraceMinutes)
                : selected.ChargedAmount ?? 0;
            var confirm = MessageBox.Show(
                $"¿Registrar la salida y cobrar?\n\nPlaca: {selected.Plate}\nMonto a cobrar: {MoneyHelper.Format(preview)}",
                "Confirmar salida",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            var closed = _controller.RegisterExit(selected.SessionId, _currentUser.UserId);
            MessageBox.Show(
                $"Salida registrada.\n\nPlaca: {closed.Plate}\nMonto a cobrar: {MoneyHelper.Format(closed.ChargedAmount ?? 0)}",
                "Cobro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            LoadGrid();
            RenderSelected(null);
        });

        AddLabeledControl(left, "Placa del vehículo", txtPlate, 24);
        AddLabeledControl(left, "Tipo de tarifa", cmbRates, 96);
        btnEntry.Location = new Point(24, 170);
        left.Controls.Add(btnEntry);

        AddLabeledControl(left, "Código QR / ticket", txtQr, 242);
        btnFindQr.Location = new Point(24, 316);
        left.Controls.Add(btnFindQr);

        AddLabeledControl(left, "Buscar por placa", txtSearchPlate, 386);
        btnFindPlate.Location = new Point(24, 460);
        left.Controls.Add(btnFindPlate);
        chkHideExited.Location = new Point(24, 516);
        left.Controls.Add(chkHideExited);

        lblDetails.Location = new Point(24, 24);
        lblDetails.Size = new Size(620, 126);
        lblDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        right.Controls.Add(lblDetails);
        btnExit.Location = new Point(24, 166);
        right.Controls.Add(btnExit);
        btnReprint.Location = new Point(214, 166);
        right.Controls.Add(btnReprint);
        grid.Location = new Point(24, 226);
        grid.Size = new Size(620, 360);
        right.Controls.Add(grid);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        pnlContent.Controls.Add(root);
        LoadGrid();
    }

    private void ShowRates()
    {
        SetActive(btnRates, "Tarifas");
        ClearContent();

        var panel = CreatePanel();
        panel.Dock = DockStyle.Fill;

        var grid = CreateGrid(DockStyle.None);
        grid.Location = new Point(24, 24);
        grid.Size = new Size(650, 590);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

        var txtName = CreateTextBox();
        var cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), Width = 260 };
        cmbType.Items.AddRange(new object[] { "Hora", "Dia", "Semana", "Mes", "Fija" });
        var txtAmount = CreateMoneyTextBox();
        txtAmount.Width = 260;
        var numGrace = new NumericUpDown { Font = new Font("Segoe UI", 11F), Maximum = 1440, Width = 260 };
        var chkActive = new CheckBox { Text = "Activa", Checked = true, Font = new Font("Segoe UI", 10F), Width = 260 };
        var btnSave = CreatePrimaryButton("Guardar tarifa");
        var btnNew = CreateSecondaryButton("Nueva");
        int editingId = 0;

        void LoadRates()
        {
            grid.DataSource = _controller.GetRates().Select(r => new
            {
                r.RateId,
                Nombre = r.Name,
                Tipo = r.RateType,
                Monto = r.Amount,
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
            chkActive.Checked = true;
        }

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
            numGrace.Value = rate.GraceMinutes;
            chkActive.Checked = rate.IsActive;
        };

        btnSave.Click += (_, _) => ExecuteThrottled(btnSave, () =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                throw new InvalidOperationException("Digite el nombre de la tarifa.");
            }

            _controller.SaveRate(new ParkingRate
            {
                RateId = editingId,
                Name = txtName.Text,
                RateType = cmbType.SelectedItem?.ToString() ?? "Hora",
                Amount = ParseMoney(txtAmount.Text),
                GraceMinutes = Convert.ToInt32(numGrace.Value),
                IsActive = chkActive.Checked,
                SortOrder = editingId
            }, _currentUser.UserId);
            LoadRates();
            ClearForm();
        });
        btnNew.Click += (_, _) => ExecuteThrottled(btnNew, ClearForm);

        panel.Controls.Add(grid);
        AddLabeledControl(panel, "Nombre", txtName, 44, 720);
        AddLabeledControl(panel, "Tipo", cmbType, 118, 720);
        AddLabeledControl(panel, "Monto", txtAmount, 192, 720);
        AddLabeledControl(panel, "Minutos de gracia", numGrace, 266, 720);
        chkActive.Location = new Point(720, 340);
        panel.Controls.Add(chkActive);
        btnSave.Location = new Point(720, 398);
        btnNew.Location = new Point(910, 398);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnNew);

        pnlContent.Controls.Add(panel);
        ClearForm();
        LoadRates();
    }

    private void ShowUsers()
    {
        SetActive(btnUsers, "Usuarios");
        ClearContent();

        var panel = CreatePanel();
        panel.Dock = DockStyle.Fill;

        var grid = CreateGrid(DockStyle.None);
        grid.Location = new Point(24, 24);
        grid.Size = new Size(650, 590);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        var txtCedula = CreateTextBox();
        var txtFullName = CreateTextBox();
        var txtPassword = CreateTextBox();
        txtPassword.PasswordChar = '*';
        var cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), Width = 260 };
        cmbRole.Items.AddRange(new object[] { "Empleado", "Administrador" });
        var chkActive = new CheckBox { Text = "Activo", Checked = true, Font = new Font("Segoe UI", 10F), Width = 260 };
        var clbPermissions = new CheckedListBox
        {
            CheckOnClick = true,
            Font = new Font("Segoe UI", 10F),
            Width = 320,
            Height = 132,
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
        AddLabeledControl(panel, "Cedula", txtCedula, 44, 720);
        AddLabeledControl(panel, "Nombre completo", txtFullName, 118, 720);
        AddLabeledControl(panel, "Contrasena", txtPassword, 192, 720);
        AddLabeledControl(panel, "Puesto", cmbRole, 266, 720);
        AddLabeledControl(panel, "Permisos", clbPermissions, 340, 720);
        chkActive.Location = new Point(720, 506);
        panel.Controls.Add(chkActive);
        btnSave.Location = new Point(720, 552);
        btnNew.Location = new Point(910, 552);
        panel.Controls.Add(btnSave);
        panel.Controls.Add(btnNew);

        pnlContent.Controls.Add(panel);
        ClearForm();
        LoadUsers();

        void SetCheckedPermissions(IEnumerable<string> permissions)
        {
            var permissionSet = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < clbPermissions.Items.Count; i++)
            {
                var item = (PermissionListItem)clbPermissions.Items[i];
                clbPermissions.SetItemChecked(i, permissionSet.Contains(item.Key));
            }
        }

        List<string> GetCheckedPermissions()
        {
            return clbPermissions.CheckedItems
                .Cast<PermissionListItem>()
                .Select(item => item.Key)
                .ToList();
        }
    }

    private void ShowClosures()
    {
        SetActive(btnClosures, "Cierres");
        ClearContent();

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
        var txtDelivered = CreateMoneyTextBox();
        var lblExpected = CreateInfoLabel("Esperado: ₡0");
        var btnExpected = CreateSecondaryButton("Calcular esperado");
        var btnEmployeeClose = CreatePrimaryButton("Cerrar empleado");

        var lblCashDate = CreateInfoLabel(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        lblCashDate.Size = new Size(300, 28);
        var denominationValues = new[] { 20000m, 10000m, 5000m, 2000m, 1000m, 500m, 100m, 50m, 25m, 10m, 5m };
        var denominationInputs = new Dictionary<decimal, NumericUpDown>();
        var denominationPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 250),
            Size = new Size(460, 272),
            AutoScroll = false,
            WrapContents = true
        };
        var baseAmount = AppSettings.MinimumCashAmount;
        decimal systemAmount = 0m;
        try { systemAmount = _controller.GetSystemCashForDate(DateTime.Today); } catch { /* se recalcula con el botón */ }

        var lblBase = CreateInfoLabel($"Fondo de caja (base): {MoneyHelper.Format(baseAmount)}");
        lblBase.ForeColor = Color.FromArgb(100, 116, 139);
        var lblSysAmt = CreateInfoLabel("Cobrado hoy (sistema): ₡0");
        var lblExpectedCash = CreateInfoLabel("Esperado en caja: ₡0");
        var lblCounted = CreateInfoLabel("Contado (físico): ₡0");
        var lblDiff = CreateInfoLabel("Diferencia: ₡0");
        foreach (var lbl in new[] { lblBase, lblSysAmt, lblExpectedCash, lblCounted, lblDiff })
        {
            lbl.Size = new Size(450, 24);
        }
        var btnSystem = CreateSecondaryButton("Recalcular");
        var btnCashClose = CreatePrimaryButton("Cerrar caja");

        foreach (var denomination in denominationValues)
        {
            var row = new Panel { Width = 205, Height = 34, Margin = new Padding(0, 0, 8, 8) };
            var label = new Label
            {
                Text = MoneyHelper.Format(denomination),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 7),
                Width = 76
            };
            var input = new NumericUpDown
            {
                Font = new Font("Segoe UI", 9F),
                Width = 104,
                Maximum = 10000,
                Location = new Point(84, 3)
            };
            input.ValueChanged += (_, _) => UpdateSummary();
            denominationInputs.Add(denomination, input);
            row.Controls.Add(label);
            row.Controls.Add(input);
            denominationPanel.Controls.Add(row);
        }

        void UpdateSummary()
        {
            var counted = denominationInputs.Sum(item => item.Key * item.Value.Value);
            var expected = systemAmount + baseAmount;
            var difference = counted - expected;
            lblSysAmt.Text = $"Cobrado hoy (sistema): {MoneyHelper.Format(systemAmount)}";
            lblExpectedCash.Text = $"Esperado en caja: {MoneyHelper.Format(expected)}";
            lblCounted.Text = $"Contado (físico): {MoneyHelper.Format(counted)}";
            var estado = difference == 0 ? "✔ Cuadra" : difference > 0 ? "▲ Sobra" : "▼ Falta";
            lblDiff.Text = $"Diferencia: {MoneyHelper.Format(difference)}    {estado}";
            lblDiff.ForeColor = difference == 0
                ? Color.FromArgb(22, 163, 74)
                : difference > 0 ? Color.FromArgb(202, 138, 4) : Color.FromArgb(220, 38, 38);
        }

        btnExpected.Click += (_, _) => ExecuteThrottled(btnExpected, () =>
        {
            if (cmbUsers.SelectedValue is int userId)
            {
                var expected = _controller.GetExpectedForUser(userId, fromPicker.Value, toPicker.Value);
                lblExpected.Text = $"Esperado: {MoneyHelper.Format(expected)}";
            }
        });

        btnEmployeeClose.Click += (_, _) => ExecuteThrottled(btnEmployeeClose, () =>
        {
            if (cmbUsers.SelectedValue is not int userId)
            {
                throw new InvalidOperationException("Seleccione un empleado.");
            }

            var deliveredAmount = ParseMoney(txtDelivered.Text);
            var closure = _controller.CreateEmployeeClosure(userId, fromPicker.Value, toPicker.Value, deliveredAmount, _currentUser.UserId);
            MessageBox.Show(
                $"Cierre creado.\nEsperado: {MoneyHelper.Format(closure.ExpectedAmount)}\nEntregado: {MoneyHelper.Format(closure.DeliveredAmount)}\nDiferencia: {MoneyHelper.Format(closure.DifferenceAmount)}",
                "Cierre de empleado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        btnSystem.Click += (_, _) => ExecuteThrottled(btnSystem, () =>
        {
            systemAmount = _controller.GetSystemCashForDate(DateTime.Today);
            UpdateSummary();
        });

        btnCashClose.Click += (_, _) => ExecuteThrottled(btnCashClose, () =>
        {
            var denominations = denominationInputs.ToDictionary(item => item.Key, item => Convert.ToInt32(item.Value.Value));
            var counted = denominations.Sum(item => item.Key * item.Value);
            var expected = systemAmount + baseAmount;
            var difference = counted - expected;
            var closureDate = DateTime.Now;
            lblCashDate.Text = closureDate.ToString("dd/MM/yyyy HH:mm");
            var closureId = _controller.CreateCashClosure(closureDate, denominations, _currentUser.UserId);
            MessageBox.Show(
                $"Cierre de caja #{closureId} creado.\n\n"
                + $"Fondo base:  {MoneyHelper.Format(baseAmount)}\n"
                + $"Sistema:     {MoneyHelper.Format(systemAmount)}\n"
                + $"Esperado:    {MoneyHelper.Format(expected)}\n"
                + $"Contado:     {MoneyHelper.Format(counted)}\n"
                + $"Diferencia:  {MoneyHelper.Format(difference)}",
                "Cierre de caja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        employeePanel.Controls.Add(CreateSectionTitle("Cierre de empleado", 24, 20));
        AddLabeledControl(employeePanel, "Empleado", cmbUsers, 76);
        AddLabeledControl(employeePanel, "Desde", fromPicker, 150);
        AddLabeledControl(employeePanel, "Hasta", toPicker, 224);
        lblExpected.Location = new Point(24, 296);
        employeePanel.Controls.Add(lblExpected);
        AddLabeledControl(employeePanel, "Monto entregado", txtDelivered, 360);
        btnExpected.Location = new Point(24, 438);
        btnEmployeeClose.Location = new Point(220, 438);
        employeePanel.Controls.Add(btnExpected);
        employeePanel.Controls.Add(btnEmployeeClose);

        cashPanel.Controls.Add(CreateSectionTitle("Cierre de caja", 24, 20));
        AddLabeledControl(cashPanel, "Fecha automatica", lblCashDate, 76);
        lblBase.Location = new Point(24, 116);
        lblSysAmt.Location = new Point(24, 140);
        lblExpectedCash.Location = new Point(24, 164);
        cashPanel.Controls.Add(lblBase);
        cashPanel.Controls.Add(lblSysAmt);
        cashPanel.Controls.Add(lblExpectedCash);
        cashPanel.Controls.Add(CreateSectionTitle("Billetes y monedas", 24, 196));
        denominationPanel.Location = new Point(24, 228);
        cashPanel.Controls.Add(denominationPanel);
        lblCounted.Location = new Point(24, 508);
        lblDiff.Location = new Point(24, 534);
        cashPanel.Controls.Add(lblCounted);
        cashPanel.Controls.Add(lblDiff);
        btnSystem.Location = new Point(24, 572);
        btnCashClose.Location = new Point(220, 572);
        cashPanel.Controls.Add(btnSystem);
        cashPanel.Controls.Add(btnCashClose);
        UpdateSummary();

        if (!_currentUser.HasPermission(PermissionKeys.CashClosure))
        {
            btnCashClose.Enabled = false;
            denominationPanel.Enabled = false;
            btnSystem.Enabled = false;
            lblSysAmt.Visible = false;
            lblExpectedCash.Visible = false;
            lblCounted.Visible = false;
            lblDiff.Visible = false;
            lblBase.Text = "El cierre de caja requiere permiso asignado.";
        }

        if (!_currentUser.HasPermission(PermissionKeys.EmployeeClosure))
        {
            cmbUsers.Enabled = false;
            fromPicker.Enabled = false;
            toPicker.Enabled = false;
            txtDelivered.Enabled = false;
            btnExpected.Enabled = false;
            btnEmployeeClose.Enabled = false;
            lblExpected.Text = "El cierre de empleado requiere permiso asignado.";
        }

        root.Controls.Add(employeePanel, 0, 0);
        root.Controls.Add(cashPanel, 1, 0);
        tabRegister.Controls.Add(root);
        BuildClosureHistoryTab(tabHistory);
        tabHistory.Enabled = _currentUser.HasPermission(PermissionKeys.ClosureHistory);
        pnlContent.Controls.Add(tabs);
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
            Width = 160
        };
        cmbType.Items.AddRange(new object[] { "Todos", "Empleado", "Caja" });
        cmbType.SelectedIndex = 0;

        var fromDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 130, Value = DateTime.Today.AddDays(-7) };
        var toDate = new DateTimePicker { Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Width = 130, Value = DateTime.Today };
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
        var btnPdf = CreatePrimaryButton("Descargar PDF");

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

        var filterPanel = new Panel { Dock = DockStyle.Fill };
        AddLabeledControl(filterPanel, "Tipo", cmbType, 4, 0);
        AddLabeledControl(filterPanel, "Desde", fromDate, 4, 200);
        AddLabeledControl(filterPanel, "Hasta", toDate, 4, 360);
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
        previewLayout.Controls.Add(btnPdf, 0, 2);
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
            // Reglas de negocio / validaciones: el mensaje es seguro de mostrar al usuario.
            MessageBox.Show(ex.Message, "Parqueo Mahischa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            // Errores inesperados (BD, sistema): mensaje genérico y registro en auditoría.
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
            var timer = new System.Windows.Forms.Timer { Interval = 5000 };
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
                $"Sistema:        {MoneyHelper.Format(record.SystemAmount)}",
                $"Minimo caja:    {MoneyHelper.Format(record.MinimumCashAmount)}",
                $"Contado fisico: {MoneyHelper.Format(record.CountedAmount)}",
                $"Diferencia:     {MoneyHelper.Format(record.DifferenceAmount)}",
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
                $"Esperado:   {MoneyHelper.Format(record.ExpectedAmount)}",
                $"Entregado:  {MoneyHelper.Format(record.DeliveredAmount)}",
                $"Diferencia: {MoneyHelper.Format(record.DifferenceAmount)}"
            ]);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void ClearContent()
    {
        pnlContent.SuspendLayout();
        pnlContent.Controls.Clear();
        pnlContent.ResumeLayout();
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
        // El botón activo mantiene el color de acento incluso al pasar el mouse.
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

        var tint = Color.FromArgb(249, 251, 253);
        var fade = new UiKit.ColorFade(panel);
        void Enter(object? s, EventArgs e) => fade.To(tint);
        void Leave(object? s, EventArgs e)
        {
            if (!panel.ClientRectangle.Contains(panel.PointToClient(Cursor.Position)))
            {
                fade.To(Color.White);
            }
        }

        panel.MouseEnter += Enter;
        panel.MouseLeave += Leave;
        foreach (Control child in panel.Controls)
        {
            child.MouseEnter += Enter;
            child.MouseLeave += Leave;
        }

        return panel;
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
        Width = 320
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


