using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using SistemaParkingMahischa.Config;
using SistemaParkingMahischa.Data;
using SistemaParkingMahischa.Models;

namespace SistemaParkingMahischa.Services;

public sealed class AuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, LoginAttempt> FailedLogins = new();

    public void EnsureDefaultAdmin()
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(1) FROM dbo.Users;";
            if (Convert.ToInt32(countCommand.ExecuteScalar()) == 0)
            {
                var (hash, salt) = SecurityService.CreatePasswordHash(AppSettings.DefaultAdminPassword);
                using var command = connection.CreateCommand();
                command.CommandText = """
                    INSERT INTO dbo.Users(Username, IdentificationNumber, PasswordHash, PasswordSalt, FullName, RoleId, IsActive, MustChangePassword)
                    SELECT @Username, @IdentificationNumber, @Hash, @Salt, @FullName, RoleId, 1, 1
                    FROM dbo.Roles
                    WHERE RoleName = N'Administrador';
                    """;
                command.Parameters.AddWithValue("@Username", AppSettings.DefaultAdminUser);
                command.Parameters.AddWithValue("@IdentificationNumber", NormalizeIdentification(AppSettings.DefaultAdminCedula));
                command.Parameters.Add("@Hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
                command.Parameters.Add("@Salt", System.Data.SqlDbType.VarBinary, 32).Value = salt;
                command.Parameters.AddWithValue("@FullName", "Administrador");
                command.ExecuteNonQuery();
            }
        }

        using (var updateExisting = connection.CreateCommand())
        {
            updateExisting.CommandText = """
                UPDATE dbo.Users
                SET IdentificationNumber = COALESCE(IdentificationNumber, Username)
                WHERE IdentificationNumber IS NULL;
                """;
            updateExisting.ExecuteNonQuery();
        }

        using (var updateDefaultAdmin = connection.CreateCommand())
        {
            updateDefaultAdmin.CommandText = """
                UPDATE u
                SET IdentificationNumber = @IdentificationNumber
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE u.Username = @Username
                  AND r.RoleName = N'Administrador'
                  AND (u.IdentificationNumber IS NULL OR u.IdentificationNumber = @Username);
                """;
            updateDefaultAdmin.Parameters.AddWithValue("@IdentificationNumber", NormalizeIdentification(AppSettings.DefaultAdminCedula));
            updateDefaultAdmin.Parameters.AddWithValue("@Username", AppSettings.DefaultAdminUser);
            updateDefaultAdmin.ExecuteNonQuery();
        }

        GrantAllPermissionsToAdministrators(connection);
    }

    public User? Login(string identificationNumber, string password)
    {
        var key = NormalizeIdentification(identificationNumber);
        EnsureNotLockedOut(key);

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                u.UserId, u.Username, u.IdentificationNumber, u.FullName, u.IsActive, u.PasswordHash, u.PasswordSalt, u.MustChangePassword, r.RoleName
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
            WHERE u.IdentificationNumber = @IdentificationNumber OR u.Username = @IdentificationNumber;
            """;
        command.Parameters.AddWithValue("@IdentificationNumber", key);

        using var reader = command.ExecuteReader();
        if (!reader.Read() || !reader.GetBoolean(reader.GetOrdinal("IsActive")))
        {
            RegisterFailedAttempt(key);
            return null;
        }

        var hash = (byte[])reader["PasswordHash"];
        var salt = (byte[])reader["PasswordSalt"];
        if (!SecurityService.VerifyPassword(password, hash, salt))
        {
            RegisterFailedAttempt(key);
            AuditService.Log(null, "LoginFallido", "Users", key, "Contraseña incorrecta");
            return null;
        }

        var user = MapUser(reader);
        user.MustChangePassword = reader.GetBoolean(reader.GetOrdinal("MustChangePassword"));
        reader.Close();
        user.Permissions = GetPermissions(connection, user.UserId);
        FailedLogins.TryRemove(key, out _);
        AuditService.Log(user.UserId, "LoginExitoso", "Users", user.UserId.ToString(), $"Ingreso de {user.FullName}");
        return user;
    }

    public bool VerifyUserPassword(int userId, string password)
    {
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PasswordHash, PasswordSalt FROM dbo.Users WHERE UserId = @UserId;";
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return false;
        }

        var hash = (byte[])reader["PasswordHash"];
        var salt = (byte[])reader["PasswordSalt"];
        return SecurityService.VerifyPassword(password, hash, salt);
    }

    public void ChangePassword(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
        {
            throw new InvalidOperationException("La nueva contraseña debe tener al menos 6 caracteres.");
        }

        var (hash, salt) = SecurityService.CreatePasswordHash(newPassword);
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Users
            SET PasswordHash = @Hash,
                PasswordSalt = @Salt,
                MustChangePassword = 0
            WHERE UserId = @UserId;
            """;
        command.Parameters.Add("@Hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
        command.Parameters.Add("@Salt", System.Data.SqlDbType.VarBinary, 32).Value = salt;
        command.Parameters.AddWithValue("@UserId", userId);
        command.ExecuteNonQuery();
        AuditService.Log(userId, "CambioContrasena", "Users", userId.ToString(), "El usuario cambió su contraseña");
    }

    private static void EnsureNotLockedOut(string key)
    {
        if (FailedLogins.TryGetValue(key, out var attempt)
            && attempt.LockedUntil is { } lockedUntil
            && lockedUntil > DateTime.UtcNow)
        {
            var seconds = (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalSeconds);
            throw new InvalidOperationException(
                $"Cuenta bloqueada por demasiados intentos fallidos. Intente de nuevo en {seconds} segundos.");
        }
    }

    private static void RegisterFailedAttempt(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        FailedLogins.AddOrUpdate(
            key,
            _ => new LoginAttempt(1, null),
            (_, existing) =>
            {
                var count = existing.LockedUntil is { } until && until <= DateTime.UtcNow ? 1 : existing.Count + 1;
                var lockedUntil = count >= MaxFailedAttempts ? DateTime.UtcNow.Add(LockoutDuration) : (DateTime?)null;
                return new LoginAttempt(count, lockedUntil);
            });
    }

    private sealed record LoginAttempt(int Count, DateTime? LockedUntil);

    public List<User> GetUsers()
    {
        var users = new List<User>();
        using var connection = SqlDatabase.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.UserId, u.Username, u.IdentificationNumber, u.FullName, u.IsActive, r.RoleName
            FROM dbo.Users u
            INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
            ORDER BY u.IsActive DESC, u.FullName;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(MapUser(reader));
        }

        reader.Close();
        foreach (var user in users)
        {
            user.Permissions = GetPermissions(connection, user.UserId);
        }

        return users;
    }

    public void SaveUser(
        int? userId,
        string identificationNumber,
        string fullName,
        string roleName,
        string? password,
        bool isActive,
        IEnumerable<string> permissions,
        int actingUserId)
    {
        var normalizedIdentification = NormalizeIdentification(identificationNumber);
        if (string.IsNullOrWhiteSpace(normalizedIdentification))
        {
            throw new InvalidOperationException("La cedula es requerida.");
        }

        using var connection = SqlDatabase.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var savedUserId = userId;
            if (userId is null)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("La contrasena es requerida para usuarios nuevos.");
                }

                var (hash, salt) = SecurityService.CreatePasswordHash(password);
                using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.Users(Username, IdentificationNumber, PasswordHash, PasswordSalt, FullName, RoleId, IsActive)
                    OUTPUT INSERTED.UserId
                    SELECT @Username, @IdentificationNumber, @Hash, @Salt, @FullName, RoleId, @IsActive
                    FROM dbo.Roles
                    WHERE RoleName = @RoleName;
                    """;
                insert.Parameters.AddWithValue("@Username", normalizedIdentification);
                insert.Parameters.AddWithValue("@IdentificationNumber", normalizedIdentification);
                insert.Parameters.Add("@Hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
                insert.Parameters.Add("@Salt", System.Data.SqlDbType.VarBinary, 32).Value = salt;
                insert.Parameters.AddWithValue("@FullName", fullName.Trim());
                insert.Parameters.AddWithValue("@RoleName", roleName);
                insert.Parameters.AddWithValue("@IsActive", isActive);
                savedUserId = Convert.ToInt32(insert.ExecuteScalar());
            }
            else
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                if (string.IsNullOrWhiteSpace(password))
                {
                    command.CommandText = """
                        UPDATE u
                        SET Username = @Username,
                            IdentificationNumber = @IdentificationNumber,
                            FullName = @FullName,
                            RoleId = r.RoleId,
                            IsActive = @IsActive
                        FROM dbo.Users u
                        CROSS JOIN dbo.Roles r
                        WHERE u.UserId = @UserId AND r.RoleName = @RoleName;
                        """;
                }
                else
                {
                    var (hash, salt) = SecurityService.CreatePasswordHash(password);
                    command.CommandText = """
                        UPDATE u
                        SET Username = @Username,
                            IdentificationNumber = @IdentificationNumber,
                            FullName = @FullName,
                            RoleId = r.RoleId,
                            IsActive = @IsActive,
                            PasswordHash = @Hash,
                            PasswordSalt = @Salt
                        FROM dbo.Users u
                        CROSS JOIN dbo.Roles r
                        WHERE u.UserId = @UserId AND r.RoleName = @RoleName;
                        """;
                    command.Parameters.Add("@Hash", System.Data.SqlDbType.VarBinary, 32).Value = hash;
                    command.Parameters.Add("@Salt", System.Data.SqlDbType.VarBinary, 32).Value = salt;
                }

                command.Parameters.AddWithValue("@UserId", userId.Value);
                command.Parameters.AddWithValue("@Username", normalizedIdentification);
                command.Parameters.AddWithValue("@IdentificationNumber", normalizedIdentification);
                command.Parameters.AddWithValue("@FullName", fullName.Trim());
                command.Parameters.AddWithValue("@RoleName", roleName);
                command.Parameters.AddWithValue("@IsActive", isActive);
                command.ExecuteNonQuery();
            }

            SavePermissions(connection, transaction, savedUserId!.Value, permissions);
            transaction.Commit();
            AuditService.Log(actingUserId, userId is null ? "CrearUsuario" : "EditarUsuario", "Users",
                savedUserId!.Value.ToString(), $"{fullName.Trim()} ({roleName}), activo={isActive}");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SavePermissions(SqlConnection connection, SqlTransaction transaction, int userId, IEnumerable<string> permissions)
    {
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM dbo.UserPermissions WHERE UserId = @UserId;";
            delete.Parameters.AddWithValue("@UserId", userId);
            delete.ExecuteNonQuery();
        }

        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO dbo.UserPermissions(UserId, PermissionKey)
                VALUES (@UserId, @PermissionKey);
                """;
            insert.Parameters.AddWithValue("@UserId", userId);
            insert.Parameters.AddWithValue("@PermissionKey", permission);
            insert.ExecuteNonQuery();
        }
    }

    private static HashSet<string> GetPermissions(SqlConnection connection, int userId)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PermissionKey FROM dbo.UserPermissions WHERE UserId = @UserId;";
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            permissions.Add(reader.GetString(0));
        }

        return permissions;
    }

    private static void GrantAllPermissionsToAdministrators(SqlConnection connection)
    {
        foreach (var permission in PermissionKeys.All)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.UserPermissions(UserId, PermissionKey)
                SELECT u.UserId, @PermissionKey
                FROM dbo.Users u
                INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
                WHERE r.RoleName = N'Administrador'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.UserPermissions up
                      WHERE up.UserId = u.UserId AND up.PermissionKey = @PermissionKey
                  );
                """;
            command.Parameters.AddWithValue("@PermissionKey", permission);
            command.ExecuteNonQuery();
        }
    }

    private static string NormalizeIdentification(string identificationNumber)
    {
        return new string(identificationNumber.Trim().Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static User MapUser(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        IdentificationNumber = reader.IsDBNull(reader.GetOrdinal("IdentificationNumber"))
            ? string.Empty
            : reader.GetString(reader.GetOrdinal("IdentificationNumber")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
    };
}
