using System.Security.Cryptography;

namespace SistemaParkingMahischa.Services;

public static class SecurityService
{
    public static (byte[] Hash, byte[] Salt) CreatePasswordHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        return (HashPassword(password, salt), salt);
    }

    public static bool VerifyPassword(string password, byte[] expectedHash, byte[] salt)
    {
        var currentHash = HashPassword(password, salt);
        return CryptographicOperations.FixedTimeEquals(currentHash, expectedHash);
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
    }
}
