using System.Text.Json.Serialization;

namespace AggregatorPlatform.Domain.Enums;

/// <summary>
/// Sens d'une transaction bancaire.
/// - BTW : Bank-to-Wallet (débit — argent qui sort du compte bancaire vers le wallet).
/// - WTB : Wallet-to-Bank (crédit — argent qui entre sur le compte bancaire depuis le wallet).
/// Sérialisation JSON en string (BTW / WTB) via <see cref="JsonStringEnumConverter"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BankTransactionDirection
{
    /// <summary>Bank-to-Wallet : débit.</summary>
    BTW = 0,

    /// <summary>Wallet-to-Bank : crédit.</summary>
    WTB = 1,
}
