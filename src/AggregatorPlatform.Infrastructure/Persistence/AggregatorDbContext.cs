using System.Reflection;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Infrastructure.Persistence;

public class AggregatorDbContext : DbContext
{
    private readonly IEncryptionService? _encryption;
    private readonly AuditSaveChangesInterceptor? _auditInterceptor;

    public AggregatorDbContext(DbContextOptions<AggregatorDbContext> options,
        IEncryptionService? encryption = null,
        AuditSaveChangesInterceptor? auditInterceptor = null) : base(options)
    {
        _encryption = encryption;
        _auditInterceptor = auditInterceptor;
    }

    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerAccount> PartnerAccounts => Set<PartnerAccount>();
    public DbSet<PartnerAccountMovement> PartnerAccountMovements => Set<PartnerAccountMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AccountingSchema> AccountingSchemas => Set<AccountingSchema>();
    public DbSet<AccountingSchemaLine> AccountingSchemaLines => Set<AccountingSchemaLine>();
    public DbSet<Movement> Movements => Set<Movement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemParameter> SystemParameters => Set<SystemParameter>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PartnerEndpoint> PartnerEndpoints => Set<PartnerEndpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            t => t.Namespace == typeof(AggregatorDbContext).Namespace + ".Configurations");

        // Encryption value converter applied where needed in entity configurations
        EncryptionValueConverter.Encryption = _encryption;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        if (_auditInterceptor is not null)
            optionsBuilder.AddInterceptors(_auditInterceptor);
    }
}
