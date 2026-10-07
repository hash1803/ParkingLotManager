using System.Security.Cryptography;
using ParkingLotManager.Data;
using ParkingLotManager.Models;

namespace ParkingLotManager.Services;

public sealed class AuthService
{
    private readonly AuthRepository _repository = new();
    public UserSession? SignIn(string username, string password)
    {
        var account = _repository.Find(username.Trim());
        if (account is null || !account.Value.Active) return null;
        var expected = Convert.FromBase64String(account.Value.Hash);
        var salt = Convert.FromBase64String(account.Value.Salt);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(expected, actual)
            ? new UserSession(account.Value.Id, account.Value.Username, account.Value.Role, account.Value.StaffId) : null;
    }
}
