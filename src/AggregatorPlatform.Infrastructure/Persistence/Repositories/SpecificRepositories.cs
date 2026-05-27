using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Infrastructure.Persistence.Repositories;

public class PartnerRepository : Repository<Partner>, IPartnerRepository
{
    public PartnerRepository(AggregatorDbContext db) : base(db) { }

    public Task<Partner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(p => p.PartnerCode == code, cancellationToken);

    public Task<Partner?> GetWithAccountAsync(Guid partnerId, CancellationToken cancellationToken = default)
        => Set.Include(p => p.PartnerAccount).FirstOrDefaultAsync(p => p.PartnerId == partnerId, cancellationToken);

    public Task<Partner?> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
        => Set.Include(p => p.PartnerAccount).FirstOrDefaultAsync(p => p.ApiKey == apiKeyHash, cancellationToken);
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AggregatorDbContext db) : base(db) { }

    public Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(c => c.ExternalCustomerId == externalId, cancellationToken);

    public Task<Customer?> GetWithSubscriptionsAsync(Guid customerId, CancellationToken cancellationToken = default)
        => Set.Include(c => c.Subscriptions).FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
}

public class PartnerEndpointRepository : Repository<PartnerEndpoint>, IPartnerEndpointRepository
{
    public PartnerEndpointRepository(AggregatorDbContext db) : base(db) { }

    public Task<PartnerEndpoint?> GetByPartnerAndKeyAsync(Guid partnerId, AggregatorPlatform.Domain.Enums.FinancialEndpointKey key, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(e => e.PartnerId == partnerId && e.EndpointKey == key, cancellationToken);

    public async Task<IReadOnlyList<PartnerEndpoint>> GetByPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default)
        => await Set.Where(e => e.PartnerId == partnerId).ToListAsync(cancellationToken);
}

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(AggregatorDbContext db) : base(db) { }

    public Task<bool> ExistsByPartnerBankAndPhoneAsync(Guid partnerId, string bankAccountNumber, string phoneNumber, CancellationToken cancellationToken = default)
        => Set.AnyAsync(s =>
            s.PartnerId == partnerId &&
            s.BankAccountNumber == bankAccountNumber &&
            s.PhoneNumber == phoneNumber,
            cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await Set.Where(s => s.CustomerId == customerId).ToListAsync(cancellationToken);
}

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AggregatorDbContext db) : base(db) { }

    public Task<Transaction?> GetByPartnerRefAsync(Guid partnerId, string partnerTransactionRef, CancellationToken cancellationToken = default)
        => Set.Include(t => t.Subscription)
              .FirstOrDefaultAsync(t => t.PartnerId == partnerId && t.PartnerTransactionRef == partnerTransactionRef, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken cancellationToken = default)
        => await Set.Where(t => t.Status == TransactionStatus.Pending && t.InitiatedAt <= threshold).ToListAsync(cancellationToken);
}

public class AccountingSchemaRepository : Repository<AccountingSchema>, IAccountingSchemaRepository
{
    public AccountingSchemaRepository(AggregatorDbContext db) : base(db) { }

    public async Task<AccountingSchema?> SelectApplicableSchemaAsync(Guid partnerId, TransactionType type,
        TransactionSide side, Channel channel, CancellationToken cancellationToken = default)
    {
        var schemas = await Set.Include(s => s.Lines)
            .Where(s => s.IsActive
                        && s.TransactionType == type
                        && s.TransactionSide == side
                        && s.Channel == channel
                        && (s.PartnerId == partnerId || s.PartnerId == null))
            .ToListAsync(cancellationToken);

        return schemas
            .OrderBy(s => s.PartnerId == null ? 1 : 0) // partner-specific first
            .ThenBy(s => s.Priority)
            .FirstOrDefault();
    }
}

public class PartnerAccountRepository : Repository<PartnerAccount>, IPartnerAccountRepository
{
    public PartnerAccountRepository(AggregatorDbContext db) : base(db) { }

    public Task<PartnerAccount?> GetByPartnerIdAsync(Guid partnerId, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(p => p.PartnerId == partnerId, cancellationToken);
}

public class WebhookLogRepository : Repository<WebhookLog>, IWebhookLogRepository
{
    public WebhookLogRepository(AggregatorDbContext db) : base(db) { }

    public async Task<IReadOnlyList<WebhookLog>> GetPendingAsync(int maxAttempts, CancellationToken cancellationToken = default)
        => await Set.Where(w => w.Status == WebhookStatus.Pending && w.AttemptCount < maxAttempts
                                && (w.NextAttemptAt == null || w.NextAttemptAt <= DateTime.UtcNow))
            .Take(100)
            .ToListAsync(cancellationToken);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AggregatorDbContext db) : base(db) { }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AggregatorDbContext db) : base(db) { }

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
}
