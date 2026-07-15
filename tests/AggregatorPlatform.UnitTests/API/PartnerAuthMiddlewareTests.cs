using AggregatorPlatform.API.Middleware;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AggregatorPlatform.UnitTests.API;

public class PartnerAuthMiddlewareTests
{
    private const string TestApiKey = "raw-api-key-123";
    private const string TestApiKeyHash = "hash-of-raw-api-key-123";

    private static (PartnerAuthMiddleware middleware, Mock<IPartnerRepository> partners, Mock<ICacheService> cache, Mock<IEncryptionService> enc, bool[] nextCalled)
        Build()
    {
        var nextCalled = new[] { false };
        var partners = new Mock<IPartnerRepository>();
        var cache = new Mock<ICacheService>();
        var enc = new Mock<IEncryptionService>();
        // ComputeSha256 mocke pour ne pas dependre du vrai algorithme.
        enc.Setup(e => e.ComputeSha256(TestApiKey)).Returns(TestApiKeyHash);
        cache.Setup(r => r.GetAsync<Partner>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partner?)null);
        var mw = new PartnerAuthMiddleware(_ =>
        {
            nextCalled[0] = true;
            return Task.CompletedTask;
        }, NullLogger<PartnerAuthMiddleware>.Instance);
        return (mw, partners, cache, enc, nextCalled);
    }

    private static HttpContext BuildContext(string path, string? host = "api.partner.com", IDictionary<string, string>? headers = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Host = new HostString(host ?? "api.partner.com");
        ctx.Response.Body = new MemoryStream();
        if (headers is not null)
            foreach (var kv in headers) ctx.Request.Headers[kv.Key] = kv.Value;
        return ctx;
    }

    [Fact]
    public async Task Returns_401_when_apikey_header_missing()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var ctx = BuildContext("/api/v1/financial/bank/balance");

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);

        ctx.Response.StatusCode.Should().Be(401);
        nextCalled[0].Should().BeFalse();
    }

    [Fact]
    public async Task Returns_401_when_apikey_unknown()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        partners.Setup(p => p.GetByApiKeyHashAsync(TestApiKeyHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partner?)null);

        var ctx = BuildContext("/api/v1/financial/bank/balance",
            headers: new Dictionary<string, string> { ["X-Partner-ApiKey"] = TestApiKey });

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);

        ctx.Response.StatusCode.Should().Be(401);
        nextCalled[0].Should().BeFalse();
    }

    [Fact]
    public async Task Returns_401_when_partner_inactive()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var partnerId = Guid.NewGuid();
        partners.Setup(p => p.GetByApiKeyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner
            {
                PartnerId = partnerId,
                BaseUrl = "https://api.partner.com",
                ApiKey = TestApiKeyHash,
                Status = PartnerStatus.Inactive,
            });

        var ctx = BuildContext("/api/v1/financial/bank/balance",
            headers: new Dictionary<string, string> { ["X-Partner-ApiKey"] = TestApiKey });

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        ctx.Response.StatusCode.Should().Be(401);
        nextCalled[0].Should().BeFalse();
    }

    [Fact]
    public async Task Bypasses_auth_paths()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var ctx = BuildContext("/api/v1/auth/login");
        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        nextCalled[0].Should().BeTrue();
    }

    [Fact]
    public async Task Bypasses_financial_admin_transactions_path()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var ctx = BuildContext("/api/v1/financial/transactions");
        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        nextCalled[0].Should().BeTrue();
    }
}
