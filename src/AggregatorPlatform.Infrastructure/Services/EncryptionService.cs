using System.Security.Cryptography;
using System.Text;
using AggregatorPlatform.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AggregatorPlatform.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        var keyB64 = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key missing");
        var ivB64 = configuration["Encryption:IV"] ?? throw new InvalidOperationException("Encryption:IV missing");
        _key = Convert.FromBase64String(keyB64);
        _iv = Convert.FromBase64String(ivB64);
        if (_key.Length != 32) throw new InvalidOperationException("Encryption key must be 32 bytes (AES-256).");
        if (_iv.Length != 16) throw new InvalidOperationException("Encryption IV must be 16 bytes.");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        using var encryptor = aes.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(plainText);
        var output = encryptor.TransformFinalBlock(input, 0, input.Length);
        return Convert.ToBase64String(output);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using var decryptor = aes.CreateDecryptor();
            var input = Convert.FromBase64String(cipherText);
            var output = decryptor.TransformFinalBlock(input, 0, input.Length);
            return Encoding.UTF8.GetString(output);
        }
        catch
        {
            return cipherText;
        }
    }

    public string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string ComputeHmacSha256(string payload, string secret)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
