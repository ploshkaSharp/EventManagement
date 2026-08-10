using System.Security.Cryptography;
using System.Text;
using EventManagement.Users.Application.Ports;

namespace EventManagement.Users.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string passwordHash)
    {
        var hash = Hash(password);
        return string.Equals(hash, passwordHash, StringComparison.OrdinalIgnoreCase);
    }
}