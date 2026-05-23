using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string ComputeSha256(string input);
    string ComputeHmacSha256(string payload, string secret);
    string GenerateApiKey();
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}

public interface IBankApiClient
{
    Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string accountNumber, CancellationToken cancellationToken = default);
    Task<BankKycResponse> GetKycAsync(Partner partner, string accountNumber, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> DebitAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> CreditAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);
}

public interface IWalletApiClient
{
    Task<WalletBalanceResponse> GetBalanceAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default);
    Task<WalletKycResponse> GetKycAsync(Partner partner, string phoneNumber, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> DebitAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> CreditAsync(Partner partner, WalletTransactionRequest request, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> CancelAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);
    Task<WalletTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);
}

public interface IWebhookService
{
    Task EnqueueAsync(Guid partnerId, Guid? transactionId, string eventType, object payload, CancellationToken cancellationToken = default);
    Task<bool> DispatchAsync(WebhookLog webhook, CancellationToken cancellationToken = default);
}

public interface IAccountingEngine
{
    Task ApplyAsync(Transaction transaction, CancellationToken cancellationToken = default);
}

public interface IFormulaEvaluator
{
    decimal EvaluateAmount(string formula, IDictionary<string, object?> context);
    bool EvaluateCondition(string condition, IDictionary<string, object?> context);
    string EvaluateExpression(string expression, IDictionary<string, object?> context);
}

public interface IFeeCalculator
{
    Task<decimal> CalculateAsync(Guid partnerId, Domain.Enums.TransactionType type, decimal amount, CancellationToken cancellationToken = default);
}

public interface ICurrentPartnerService
{
    Guid? PartnerId { get; }
    Partner? Current { get; }
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string token);
}

public interface ITwoFactorService
{
    string GenerateSecret();
    bool ValidateCode(string secret, string code);
    string GetQrCodeUri(string secret, string username, string issuer);
}

// ---- DTOs partagés avec les clients HTTP ----

public record BankBalanceResponse(string AccountNumber, decimal Balance, string Currency, string Status);
public record BankKycResponse(string AccountNumber, string FullName, string Status, string KycLevel);
public record BankTransactionRequest(string PartnerRef, string AccountNumber, decimal Amount, string Currency, string? Description);
public record BankTransactionResponse(string ExternalRef, string Status, string? FailureReason);

public record WalletBalanceResponse(string PhoneNumber, decimal Balance, string Currency, string Status);
public record WalletKycResponse(string PhoneNumber, string FullName, string Status, string KycLevel);
public record WalletTransactionRequest(string PartnerRef, string PhoneNumber, decimal Amount, string Currency, string? Description);
public record WalletTransactionResponse(string ExternalRef, string Status, string? FailureReason);
