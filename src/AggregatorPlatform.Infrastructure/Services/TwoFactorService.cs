using AggregatorPlatform.Application.Interfaces;
using OtpNet;

namespace AggregatorPlatform.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    public string GenerateSecret()
    {
        var bytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(bytes);
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code)) return false;
        try
        {
            var bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
        }
        catch { return false; }
    }

    public string GetQrCodeUri(string secret, string username, string issuer)
        => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(username)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
}
