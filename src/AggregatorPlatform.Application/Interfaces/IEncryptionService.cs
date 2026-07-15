namespace AggregatorPlatform.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string ComputeSha256(string input);
    string ComputeHmacSha256(string payload, string secret);
    string GenerateApiKey();
}
