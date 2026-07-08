using SistemaParkingMahischa.Controllers;
using SistemaParkingMahischa.Helpers;
using SistemaParkingMahischa.Models;
using SistemaParkingMahischa.Services;

namespace SistemaParkingMahischa.Forms;

/// <summary>
/// Asistente de cierre del día en dos pasos, pensado para hacerse con el mínimo de clics:
/// Paso 1 — se cuenta el dinero cobrado y se entrega (cierre de empleado, con tiquete).
/// Paso 2 — se cuenta lo que queda en la caja, el fondo (cierre de caja, con tiquete).
/// Cada tiquete se imprime automáticamente al continuar.
/// </summary>
public sealed class ClosureWizardForm : Form
{
    private static readonly decimal[] DenominationValues = [20000m, 10000m, 5000m, 2000m, 1000m, 500m, 100m, 50m, 25m, 10m, 5m];

    private readonly ParkingController _controller;
    private readonly User _currentUser;
    private readonly TicketService _ticketService;
    private readonly bool _includeEmployee;
    private readonly bool _includeCash;

    /// <summary>Indica si se creó al menos un cierre (para refrescar la pantalla anterior).</summary>
    public bool ClosuresCreated { get; private set; }

    private readonly Panel? _step1;
    private readonly Panel? _step2;

    /// <summary>
    /// El asistente se adapta a los permisos del usuario: si solo puede hacer el cierre de
    /// empleado o solo el de caja, se muestra únicamente ese paso.
    /// </summary>
    public ClosureWizardForm(ParkingController controller, User currentUser, TicketService ticketService, bool includeEmployee, bool includeCash)
    {
        _controller = controller;
        _currentUser = currentUser;
        _ticketService = ticketService;
        _includeEmployee = includeEmployee;
        _includeCash = includeCash;

        Text = "Cierre del día";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 606);
        BackColor = Color.White;
        Icon = BrandAssets.Icon;
        Font = new Font("Segoe UI", 10F);

        var accent = new Panel { BackColor = Color.FromArgb(0, 128, 117), Dock = DockStyle.Top, Height = 6 };
        Controls.Add(accent);

        _step1 = _includeEmployee ? BuildEmployeeStep() : null;
        _step2 = _includeCash ? BuildCashStep() : null;
        if (_step1 is not null)
        {
            Controls.Add(_step1);
        }

        if (_step2 is not null)
        {
            _step2.Visible = _step1 is null;
            Controls.Add(_step2);
            if (_step1 is null)
            {
                _refreshCashStep?.Invoke();
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // Paso 1: cierre de empleado (entregar lo cobrado)
    // ---------------------------------------------------------------------------------------

    private Panel BuildEmployeeStep()
    {
        var panel = MakeStepPanel();
        AddHeader(panel, _includeCash ? "PASO 1 DE 2" : "CIERRE DE EMPLEADO", "Entregar el dinero cobrado",
            "Cuente el dinero COBRADO en el turno (lo que se va a entregar) y digite cuántos billetes y monedas hay de cada tipo. El fondo de la caja NO se cuenta aquí.");

        var cmbUsers = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 11F),
            Width = 250,
            DisplayMember = nameof(User.FullName),
            ValueMember = nameof(User.UserId),
            DataSource = _currentUser.IsAdministrator ? _controller.GetUsers() : new List<User> { _currentUser }
        };
        cmbUsers.SelectedValue = _currentUser.UserId;
        var fromPicker = new DateTimePicker { Font = new Font("Segoe UI", 10F), Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM HH:mm", Value = DateTime.Today };
        var toPicker = new DateTimePicker { Font = new Font("Segoe UI", 10F), Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM HH:mm", Value = DateTime.Now };

        AddCaption(panel, "Empleado", new Point(24, 132));
        cmbUsers.Location = new Point(24, 154);
        AddCaption(panel, "Desde", new Point(300, 132));
        fromPicker.Location = new Point(300, 154);
        AddCaption(panel, "Hasta", new Point(456, 132));
        toPicker.Location = new Point(456, 154);
        panel.Controls.Add(cmbUsers);
        panel.Controls.Add(fromPicker);
        panel.Controls.Add(toPicker);

        var lblExpected = MakeInfoLabel(new Point(24, 194));
        panel.Controls.Add(lblExpected);

        var (denomPanel, denomInputs) = CreateDenominationPanel();
        denomPanel.Location = new Point(24, 226);
        panel.Controls.Add(denomPanel);

        var lblCounted = MakeBigLabel(new Point(24, 232 + denomPanel.Height));
        var lblDiff = MakeBigLabel(new Point(24, 264 + denomPanel.Height));
        panel.Controls.Add(lblCounted);
        panel.Controls.Add(lblDiff);

        var btnContinue = MakeActionButton(_includeCash ? "Continuar  ➜" : "Finalizar cierre  ✔", new Point(24, 308 + denomPanel.Height));
        panel.Controls.Add(btnContinue);

        var cashExpected = 0m;

        void UpdateTotals()
        {
            var counted = denomInputs.Sum(item => item.Key * ParseQuantity(item.Value));
            lblCounted.Text = $"Entregado (contado): {MoneyHelper.Format(counted)}";
            var difference = counted - cashExpected;
            var estado = difference == 0 ? "✔ Cuadra" : difference > 0 ? "▲ Sobra" : "▼ Falta";
            lblDiff.Text = $"Diferencia: {MoneyHelper.Format(difference)}    {estado}";
            lblDiff.ForeColor = difference == 0
                ? Color.FromArgb(22, 163, 74)
                : difference > 0 ? Color.FromArgb(202, 138, 4) : Color.FromArgb(220, 38, 38);
        }

        void RecalculateExpected()
        {
            if (cmbUsers.SelectedValue is not int userId)
            {
                return;
            }

            var totals = _controller.GetUserTotals(userId, fromPicker.Value, toPicker.Value);
            cashExpected = totals.Cash;
            lblExpected.Text = $"Efectivo a entregar: {MoneyHelper.Format(totals.Cash)}      SINPE cobrado (no se entrega): {MoneyHelper.Format(totals.Sinpe)}";
            UpdateTotals();
        }

        foreach (var input in denomInputs.Values)
        {
            input.TextChanged += (_, _) => UpdateTotals();
        }

        cmbUsers.SelectedIndexChanged += (_, _) => Safely(RecalculateExpected);
        fromPicker.ValueChanged += (_, _) => Safely(RecalculateExpected);
        toPicker.ValueChanged += (_, _) => Safely(RecalculateExpected);

        btnContinue.Click += (_, _) => Safely(() =>
        {
            if (cmbUsers.SelectedValue is not int userId)
            {
                throw new InvalidOperationException("Seleccione un empleado.");
            }

            var denominations = denomInputs.ToDictionary(item => item.Key, item => ParseQuantity(item.Value));
            var counted = denominations.Sum(item => item.Key * item.Value);
            if (counted != cashExpected)
            {
                var confirm = MessageBox.Show(
                    $"El dinero contado ({MoneyHelper.Format(counted)}) no cuadra con el efectivo esperado ({MoneyHelper.Format(cashExpected)}).\n\n¿Desea continuar de todos modos?",
                    "Cierre de empleado",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            btnContinue.Enabled = false;
            try
            {
                var closure = _controller.CreateEmployeeClosure(userId, fromPicker.Value, toPicker.Value, denominations, _currentUser.UserId);
                ClosuresCreated = true;
                PrintClosureSafely(new ClosureHistoryRecord
                {
                    ClosureType = "Empleado",
                    ClosureId = closure.ClosureId,
                    EmployeeName = cmbUsers.Text,
                    CreatedByName = _currentUser.FullName,
                    FromAt = closure.FromAt,
                    ToAt = closure.ToAt,
                    CreatedAt = closure.CreatedAt,
                    ExpectedAmount = closure.ExpectedAmount,
                    DeliveredAmount = closure.DeliveredAmount,
                    DifferenceAmount = closure.DifferenceAmount,
                    CashAmount = closure.CashExpected,
                    SinpeAmount = closure.SinpeExpected,
                    Denominations = BuildDenominationDetails(denominations)
                });

                if (_step2 is not null)
                {
                    _step1!.Visible = false;
                    _step2.Visible = true;
                    _refreshCashStep?.Invoke();
                }
                else
                {
                    MessageBox.Show(
                        "¡Listo! El cierre quedó registrado y el tiquete se envió a la impresora.",
                        "Cierre de empleado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            finally
            {
                btnContinue.Enabled = true;
            }
        });

        panel.VisibleChanged += (_, _) =>
        {
            if (panel.Visible)
            {
                toPicker.Value = DateTime.Now;
                Safely(RecalculateExpected);
            }
        };
        Safely(RecalculateExpected);

        return panel;
    }

    // ---------------------------------------------------------------------------------------
    // Paso 2: cierre de caja (contar el fondo que queda)
    // ---------------------------------------------------------------------------------------

    private Action? _refreshCashStep;

    private Panel BuildCashStep()
    {
        var panel = MakeStepPanel();
        AddHeader(panel, _includeEmployee ? "PASO 2 DE 2" : "CIERRE DE CAJA", "Contar el fondo de la caja",
            "Ahora cuente el dinero que QUEDA en la caja (el fondo para vueltos) y digite cuántos billetes y monedas hay de cada tipo.");

        var lblFondo = MakeInfoLabel(new Point(24, 132));
        panel.Controls.Add(lblFondo);

        var (denomPanel, denomInputs) = CreateDenominationPanel();
        denomPanel.Location = new Point(24, 164);
        panel.Controls.Add(denomPanel);

        var lblCounted = MakeBigLabel(new Point(24, 170 + denomPanel.Height));
        var lblDiff = MakeBigLabel(new Point(24, 202 + denomPanel.Height));
        panel.Controls.Add(lblCounted);
        panel.Controls.Add(lblDiff);

        var btnFinish = MakeActionButton("Finalizar cierre  ✔", new Point(24, 246 + denomPanel.Height));
        panel.Controls.Add(btnFinish);

        var fondo = 0m;
        var cashSystem = 0m;
        var sinpeSystem = 0m;

        void UpdateTotals()
        {
            var counted = denomInputs.Sum(item => item.Key * ParseQuantity(item.Value));
            lblCounted.Text = $"Contado (físico): {MoneyHelper.Format(counted)}";
            var difference = counted - fondo;
            var estado = difference == 0 ? "✔ Cuadra" : difference > 0 ? "▲ Sobra" : "▼ Falta";
            lblDiff.Text = $"Diferencia: {MoneyHelper.Format(difference)}    {estado}";
            lblDiff.ForeColor = difference == 0
                ? Color.FromArgb(22, 163, 74)
                : difference > 0 ? Color.FromArgb(202, 138, 4) : Color.FromArgb(220, 38, 38);
        }

        void Recalculate()
        {
            fondo = ConfigService.GetMinimumCashAmount();
            var totals = _controller.GetSummaryForDate(DateTime.Today);
            cashSystem = totals.Cash;
            sinpeSystem = totals.Sinpe;
            lblFondo.Text = $"Fondo de caja (lo que debe quedar): {MoneyHelper.Format(fondo)}";
            UpdateTotals();
        }

        foreach (var input in denomInputs.Values)
        {
            input.TextChanged += (_, _) => UpdateTotals();
        }

        _refreshCashStep = () => Safely(Recalculate);

        btnFinish.Click += (_, _) => Safely(() =>
        {
            var denominations = denomInputs.ToDictionary(item => item.Key, item => ParseQuantity(item.Value));
            var counted = denominations.Sum(item => item.Key * item.Value);
            if (counted != fondo)
            {
                var confirm = MessageBox.Show(
                    $"El dinero contado ({MoneyHelper.Format(counted)}) no cuadra con el fondo de caja ({MoneyHelper.Format(fondo)}).\n\n¿Desea continuar de todos modos?",
                    "Cierre de caja",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            btnFinish.Enabled = false;
            try
            {
                var closureId = _controller.CreateCashClosure(DateTime.Now, denominations, _currentUser.UserId);
                ClosuresCreated = true;
                PrintClosureSafely(new ClosureHistoryRecord
                {
                    ClosureType = "Caja",
                    ClosureId = closureId,
                    CreatedByName = _currentUser.FullName,
                    CreatedAt = DateTime.Now,
                    MinimumCashAmount = fondo,
                    SystemAmount = cashSystem,
                    CashAmount = cashSystem,
                    SinpeAmount = sinpeSystem,
                    CountedAmount = counted,
                    DifferenceAmount = counted - fondo,
                    Denominations = BuildDenominationDetails(denominations)
                });

                MessageBox.Show(
                    _includeEmployee
                        ? "¡Listo! El cierre del día quedó registrado y los tiquetes se enviaron a la impresora."
                        : "¡Listo! El cierre de caja quedó registrado y el tiquete se envió a la impresora.",
                    "Cierre del día",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                btnFinish.Enabled = true;
            }
        });

        return panel;
    }

    // ---------------------------------------------------------------------------------------
    // Ayudantes de interfaz
    // ---------------------------------------------------------------------------------------

    private Panel MakeStepPanel() => new()
    {
        Location = new Point(0, 6),
        Size = new Size(ClientSize.Width, ClientSize.Height - 6),
        BackColor = Color.White,
        AutoScroll = true
    };

    private static void AddHeader(Panel panel, string step, string title, string instructions)
    {
        panel.Controls.Add(new Label
        {
            Text = step,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 128, 117),
            Location = new Point(24, 18),
            AutoSize = true
        });
        panel.Controls.Add(new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(22, 40),
            AutoSize = true
        });
        panel.Controls.Add(new Label
        {
            Text = instructions,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(71, 85, 105),
            Location = new Point(24, 80),
            Size = new Size(572, 48)
        });
    }

    private static void AddCaption(Panel panel, string text, Point location) => panel.Controls.Add(new Label
    {
        Text = text,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(71, 85, 105),
        Location = location,
        AutoSize = true
    });

    private static Label MakeInfoLabel(Point location) => new()
    {
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(30, 41, 59),
        Location = location,
        Size = new Size(572, 24)
    };

    private static Label MakeBigLabel(Point location) => new()
    {
        Font = new Font("Segoe UI", 13F, FontStyle.Bold),
        ForeColor = Color.FromArgb(30, 41, 59),
        Location = location,
        Size = new Size(572, 28)
    };

    private Button MakeActionButton(string text, Point location)
    {
        var button = new Button
        {
            Text = text,
            Location = location,
            Size = new Size(572, 56),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(22, 163, 74),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        UiKit.RoundCorners(button, 10);
        UiKit.AttachHover(button, Color.FromArgb(22, 163, 74), Color.FromArgb(18, 135, 62));
        return button;
    }

    /// <summary>
    /// Casillas grandes para contar billetes y monedas: solo se escribe la cantidad con el
    /// teclado (sin flechitas), en letra grande para que sea fácil de leer.
    /// </summary>
    private static (Panel panel, Dictionary<decimal, TextBox> inputs) CreateDenominationPanel()
    {
        var inputs = new Dictionary<decimal, TextBox>();
        var flow = new FlowLayoutPanel
        {
            Size = new Size(576, 190),
            AutoScroll = false,
            WrapContents = true
        };

        foreach (var denomination in DenominationValues)
        {
            var row = new Panel { Width = 186, Height = 40, Margin = new Padding(0, 0, 4, 6) };
            var label = new Label
            {
                Text = MoneyHelper.Format(denomination),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 9),
                Width = 96,
                TextAlign = ContentAlignment.MiddleRight
            };
            var input = new TextBox
            {
                Font = new Font("Segoe UI", 12F),
                Width = 74,
                MaxLength = 4,
                Location = new Point(102, 4),
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle
            };
            input.KeyPress += (_, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
            inputs.Add(denomination, input);
            row.Controls.Add(label);
            row.Controls.Add(input);
            flow.Controls.Add(row);
        }

        return (flow, inputs);
    }

    private static int ParseQuantity(TextBox input) =>
        int.TryParse(input.Text, out var value) && value > 0 ? value : 0;

    private static List<CashDenominationDetail> BuildDenominationDetails(IReadOnlyDictionary<decimal, int> denominations) =>
        denominations.Where(item => item.Value > 0)
            .OrderByDescending(item => item.Key)
            .Select(item => new CashDenominationDetail { Denomination = item.Key, Quantity = item.Value })
            .ToList();

    /// <summary>El cierre queda guardado aunque la impresión falle: solo se avisa del problema.</summary>
    private void PrintClosureSafely(ClosureHistoryRecord record)
    {
        try
        {
            _ticketService.PrintClosureTicket(record);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"El cierre quedó guardado, pero no se pudo imprimir el tiquete.\n\nDetalle: {ex.Message}\n\nPuede reimprimirlo desde Cierres → Historial.",
                "Impresión",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void Safely(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Cierre del día", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AuditService.Log(_currentUser.UserId, "Error", "CierreAsistido", null, $"{ex.GetType().Name}: {ex.Message}");
            MessageBox.Show(
                "Ocurrió un error inesperado. Intente de nuevo; si el problema continúa, contacte al administrador.",
                "Cierre del día",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UiKit.FadeIn(this);
    }
}
