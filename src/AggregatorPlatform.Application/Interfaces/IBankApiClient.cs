using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Client du connecteur bancaire externe (projet bank_connector).
/// Contrat aligne 1:1 sur les endpoints exposes par le connecteur :
///   POST /bank/balance         -> <see cref="GetBalanceAsync"/>
///   POST /bank/kyc             -> <see cref="GetKycAsync"/>
///   POST /bank/transaction     -> <see cref="TransactionAsync"/>       (debit + credit unifies via CodOpsc)
///   POST /bank/insertmouvement -> <see cref="InsertMouvementAsync"/>
/// Aucune methode /bank/status : le connecteur ne l'expose pas.
/// </summary>
public interface IBankApiClient
{
    Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string bankAccount, CancellationToken cancellationToken = default);
    Task<BankKycDto> GetKycAsync(Partner partner, BankKycRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> TransactionAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionResponse> InsertMouvementAsync(Partner partner, IReadOnlyList<BankMouvementLine> mouvements, CancellationToken cancellationToken = default);
}
