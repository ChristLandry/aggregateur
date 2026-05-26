using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Interfaces;
using AggregatorPlatform.Infrastructure.BackgroundJobs;
using AggregatorPlatform.Infrastructure.HttpClients;
using AggregatorPlatform.Infrastructure.Persistence;
using AggregatorPlatform.Infrastructure.Persistence.Interceptors;
using AggregatorPlatform.Infrastructure.Persistence.Repositories;
using AggregatorPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace AggregatorPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AggregatorDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AggregatorDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountingSchemaRepository, AccountingSchemaRepository>();
        services.AddScoped<IPartnerAccountRepository, PartnerAccountRepository>();
        services.AddScoped<IWebhookLogRepository, WebhookLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IFormulaEvaluator, FormulaEvaluator>();
        services.AddScoped<IAccountingEngine, AccountingEngine>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddSingleton<ITwoFactorService, TwoFactorService>();

        // In-memory cache (replaces Redis)
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // HTTP clients with Polly
        services.AddHttpClient("PartnerBank")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddHttpClient("PartnerWallet")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddHttpClient("Webhook")
            .AddPolicyHandler(GetRetryPolicy());

        services.AddScoped<IBankApiClient, BankApiClient>();
        services.AddScoped<IWalletApiClient, WalletApiClient>();

        // Background jobs
        services.AddHostedService<ReconciliationJob>();
        services.AddHostedService<WebhookDispatchJob>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry - 1)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(60));
}
