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
        services.AddScoped<IPartnerEndpointRepository, PartnerEndpointRepository>();
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

        // Connecteurs wallet : registres en concret + resolver qui choisit selon Partner.PartnerCode.
        // IWalletApiClient reste bind vers le generique par defaut (retro-compat pour jobs de fond
        // comme ReconciliationJob qui n'ont pas de Partner ambient au moment du GetRequiredService).
        services.AddScoped<WalletApiClient>();
        services.AddScoped<WaveLinkedAccountConnector>();
        services.AddScoped<IWalletApiClient>(sp => sp.GetRequiredService<WalletApiClient>());
        services.AddScoped<IWalletConnectorResolver, WalletConnectorResolver>();

        // Façade Wave externe (Aggregator.WaveConnector.Api) : HttpClient nommé + client typé.
        // La BaseAddress N'EST PAS configurée ici : chaque appel construit son URL absolue
        // à partir de Partner.BaseUrl (chaque partenaire pointe sur sa propre instance).
        // Seul le header X-Partner-Id (partagé, sensible) et les policies Polly sont ici.
        services.Configure<WaveConnectorOptions>(configuration.GetSection(WaveConnectorOptions.SectionName));
        services.AddHttpClient(WaveConnectorHttpClient.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WaveConnectorOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new InvalidOperationException($"Configuration manquante : {WaveConnectorOptions.SectionName}:ApiKey.");
            // La façade Wave exige X-Partner-Id sur /api/wave/*.
            client.DefaultRequestHeaders.Add("X-Partner-Id", opts.ApiKey);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddScoped<IWaveConnectorClient, WaveConnectorHttpClient>();

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
