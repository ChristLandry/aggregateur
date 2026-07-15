using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Domain.Interfaces;

public interface IPartnerRepository : IRepository<Partner>
{
    Task<Partner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Partner?> GetWithAccountAsync(Guid partnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupere un partenaire actif a partir du SHA-256 de sa cle API en clair
    /// (Partner.ApiKey est stocke deja hashe).
    /// </summary>
    Task<Partner?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithSubscriptionsAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// Vrai si une souscription active existe deja pour ce triplet exact
    /// (PartnerId, BankAccount, PhoneNumber).
    /// Regle metier : seule la combinaison complete est interdite ;
    /// reutiliser le meme bank account avec un autre phone (et inversement) est autorise,
    /// y compris chez le meme partenaire.
    /// </summary>
    Task<bool> ExistsByPartnerBankAndPhoneAsync(Guid partnerId, string bankAccount, string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupere la souscription ACTIVE d'un partenaire pour un couple (phoneNumber, bankAccount).
    /// Utilise par les endpoints /api/v1/bank/* pour identifier la souscription cible
    /// sans passer par un SubscriptionId explicite.
    /// </summary>
    Task<Subscription?> GetActiveSubscriptionByPartnerAndContactAsync(Guid partnerId, string phoneNumber, string bankAccount, CancellationToken cancellationToken = default);

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

public interface IPartnerEndpointRepository : IRepository<PartnerEndpoint>
{
    Task<PartnerEndpoint?> GetByPartnerAndKeyAsync(Guid partnerId, AggregatorPlatform.Domain.Enums.FinancialEndpointKey key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartnerEndpoint>> GetByPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default);
}
