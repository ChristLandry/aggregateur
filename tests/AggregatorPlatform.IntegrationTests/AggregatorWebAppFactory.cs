using System.Net.Http.Headers;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AggregatorPlatform.IntegrationTests;

public class AggregatorWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with InMemory
            var ctxDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AggregatorDbContext>));
            if (ctxDescriptor != null) services.Remove(ctxDescriptor);
            services.AddDbContext<AggregatorDbContext>(opts => opts.UseInMemoryDatabase("integration-tests"));

            // Replace cache with a simple stub (the in-memory cache is already in the host,
            // but this stub keeps integration tests deterministic across runs).
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);
            services.AddSingleton<ICacheService, FakeCacheService>();

            // Stub external HTTP clients
            services.AddSingleton(Mock.Of<IBankApiClient>());
            services.AddSingleton(Mock.Of<IWalletApiClient>());
        });
    }

    /// <summary>
    /// Cree un HttpClient pre-decore avec un Bearer token et un X-Partner-ApiKey
    /// factices pour les tests qui exercent les routes authentifiees.
    /// La validation JWT reelle reste active : les tests qui en dependent doivent
    /// remplacer ce stub par un token signe avec la cle de test.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "integration-test-token");
        client.DefaultRequestHeaders.Add("X-Partner-ApiKey", "integration-test-apikey");
        return client;
    }
}

internal sealed class FakeCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<T?>(default);

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<long> IncrementAsync(string key, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        => Task.FromResult(1L);
}
