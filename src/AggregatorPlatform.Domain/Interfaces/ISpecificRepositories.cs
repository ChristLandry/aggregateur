using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Domain.Interfaces;

public interface IPartnerRepository : IRepository<Partner>
{
    Task<Partner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Partner?> GetWithAccountAsync(Guid partnerId, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithSubscriptionsAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// Vrai si une souscription active existe deja pour ce partenaire avec ce numero de telephone.
    /// Regle metier : une souscription est unique par (PartnerId, PhoneNumber).
    /// </summary>
    Task<bool> ExistsByPartnerAndPhoneAsync(Guid partnerId, string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vrai si une souscription active existe deja pour ce partenaire avec ce numero de compte.
    /// Regle metier : une souscription est unique par (PartnerId, BankAccountNumber).
    /// </summary>
    Task<bool> ExistsByPartnerAndBankAccountAsync(Guid partnerId, string bankAccountNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<Transaction?> GetByPartnerRefAsync(Guid partnerId, string partnerTransactionRef, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default);
}

public interface IAccountingSchemaRepository : IRepository<AccountingSchema>
{
    Task<AccountingSchema?> SelectApplicableSchemaAsync(Guid partnerId, AggregatorPlatform.Domain.Enums.TransactionType type,
        AggregatorPlatform.Domain.Enums.TransactionSide side, AggregatorPlatform.Domain.Enums.Channel channel,
        CancellationToken cancellationToken = default);
}

public interface IPartnerAccountRepository : IRepository<PartnerAccount>
{
    Task<PartnerAccount?> GetByPartnerIdAsync(Guid partnerId, CancellationToken cancellationToken = default);
}

public interface IWebhookLogRepository : IRepository<WebhookLog>
{
    Task<IReadOnlyList<WebhookLog>> GetPendingAsync(int maxAttempts, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
