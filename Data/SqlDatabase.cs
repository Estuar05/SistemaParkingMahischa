using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Config;

namespace SistemaParkingMahischa.Data;

public static class SqlDatabase
{
    public static SqlConnection CreateConnection() => new(AppSettings.ConnectionString);

    public static SqlConnection CreateMasterConnection() => new(AppSettings.MasterConnectionString);

    public static void ExecuteBatch(SqlConnection connection, string sql, SqlTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 0;
        command.Transaction = transaction;
        command.ExecuteNonQuery();
    }
}

