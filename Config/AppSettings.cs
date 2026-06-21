using System.Configuration;
using Microsoft.Data.SqlClient;

namespace SistemaParkingMahischa.Config;

public static class AppSettings
{
    private const string FallbackConnectionString =
        "Data Source=.\\SQLEXPRESS;Initial Catalog=ParqueoMaishaDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Application Name=\"SistemaParkingMahischa\";Command Timeout=0";

    public static string ConnectionString
    {
        get
        {
            var configured = ConfigurationManager.ConnectionStrings["ParqueoMaishaDB"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(configured))
            {
                return FallbackConnectionString;
            }

            var builder = new SqlConnectionStringBuilder(configured);
            if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                builder.InitialCatalog = "ParqueoMaishaDB";
            }

            return builder.ConnectionString;
        }
    }

    public static string MasterConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ConnectionString;
        }
    }

    public static string DatabaseName
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "ParqueoMaishaDB" : builder.InitialCatalog;
        }
    }

    public static string BusinessName => ConfigurationManager.AppSettings["BusinessName"] ?? "Parqueo Mahischa";

    public static string ContactPhone => ConfigurationManager.AppSettings["ContactPhone"] ?? "+506 8687 5906 / +506 8366 9729";

    public static string UpdateRepository => ConfigurationManager.AppSettings["UpdateRepository"] ?? string.Empty;

    public static string DefaultAdminUser => ConfigurationManager.AppSettings["DefaultAdminUser"] ?? "admin";

    public static string DefaultAdminCedula => ConfigurationManager.AppSettings["DefaultAdminCedula"] ?? "000000000";

    public static string DefaultAdminPassword => ConfigurationManager.AppSettings["DefaultAdminPassword"] ?? "admin123";

    public static decimal MinimumCashAmount =>
        decimal.TryParse(ConfigurationManager.AppSettings["MinimumCashAmount"], out var value) ? value : 50000m;
}
