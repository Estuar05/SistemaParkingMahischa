namespace SistemaParkingMahischa.Models;

public sealed class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsAdministrator => RoleName.Equals("Administrador", StringComparison.OrdinalIgnoreCase);

    public bool HasPermission(string permissionKey) => IsAdministrator || Permissions.Contains(permissionKey);
}

public static class PermissionKeys
{
    public const string Dashboard = "Dashboard";
    public const string Parking = "Parking";
    public const string Rates = "Rates";
    public const string Users = "Users";
    public const string Income = "Income";
    public const string EmployeeClosure = "EmployeeClosure";
    public const string CashClosure = "CashClosure";
    public const string ClosureHistory = "ClosureHistory";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Dashboard] = "Panel",
        [Parking] = "Entrada / salida",
        [Rates] = "Tarifas",
        [Users] = "Usuarios",
        [Income] = "Ingresos",
        [EmployeeClosure] = "Cierre de empleado",
        [CashClosure] = "Cierre de caja",
        [ClosureHistory] = "Historial de cierres"
    };

    public static readonly string[] All =
    [
        Dashboard,
        Parking,
        Rates,
        Users,
        Income,
        EmployeeClosure,
        CashClosure,
        ClosureHistory
    ];
}
