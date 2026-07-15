using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface IBankApiClient
{
    Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string accountNumber, CancellationToken cancellationToken = default);
    Task<BankKycDto> GetKycAsync(Partner partner, BankKycRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> DebitAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> CreditAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> GetStatusAsync(Partner partner, string externalRef, CancellationToken cancellationToken = default);
}
