namespace AggregatorPlatform.Application.Interfaces;

public interface ITwoFactorService
{
    string GenerateSecret();
    bool ValidateCode(string secret, string code);
    string GetQrCodeUri(string secret, string username, string issuer);
}
