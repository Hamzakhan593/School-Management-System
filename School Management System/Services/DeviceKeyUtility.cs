using System.Security.Cryptography;
using System.Text;

namespace School_Management_System.Services;

public static class DeviceKeyUtility
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Verify(string suppliedKey, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(suppliedKey) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var suppliedHash = Hash(suppliedKey);
        var left = Encoding.ASCII.GetBytes(suppliedHash);
        var right = Encoding.ASCII.GetBytes(storedHash);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
