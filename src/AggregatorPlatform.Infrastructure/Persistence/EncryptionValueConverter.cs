using AggregatorPlatform.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AggregatorPlatform.Infrastructure.Persistence;

public static class EncryptionValueConverter
{
    public static IEncryptionService? Encryption { get; set; }

    public static ValueConverter<string, string> ForString() => new(
        v => Encryption == null || string.IsNullOrEmpty(v) ? v : Encryption.Encrypt(v),
        v => Encryption == null || string.IsNullOrEmpty(v) ? v : Encryption.Decrypt(v));

    public static ValueConverter<string?, string?> ForNullableString() => new(
        v => Encryption == null || string.IsNullOrEmpty(v) ? v : Encryption.Encrypt(v),
        v => Encryption == null || string.IsNullOrEmpty(v) ? v : Encryption.Decrypt(v));
}
