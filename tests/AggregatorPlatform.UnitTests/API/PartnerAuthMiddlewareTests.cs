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
    private static (PartnerAuthMiddleware middleware, Mock<IPartnerRepository> partners, Mock<ICacheService> cache, Mock<IEncryptionService> enc, bool[] nextCalled)
        Build()
    {
        var nextCalled = new[] { false };
        var partners = new Mock<IPartnerRepository>();
        var cache = new Mock<ICacheService>();
        var enc = new Mock<IEncryptionService>();
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
    public async Task Returns_401_when_partner_id_header_missing()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var ctx = BuildContext("/api/v1/financial/bank/balance");

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);

        ctx.Response.StatusCode.Should().Be(401);
        nextCalled[0].Should().BeFalse();
    }

    [Fact]
    public async Task Returns_401_when_partner_inactive()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var partnerId = Guid.NewGuid();
        partners.Setup(p => p.GetWithAccountAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner
            {
                PartnerId = partnerId,
                BaseUrl = "https://api.partner.com",
                Status = PartnerStatus.Inactive,
                RateLimitPerMin = 100
            });
        cache.Setup(r => r.GetAsync<Partner>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Partner?)null);

        var ctx = BuildContext("/api/v1/financial/bank/balance",
            headers: new Dictionary<string, string> { ["X-Partner-Id"] = partnerId.ToString() });

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        ctx.Response.StatusCode.Should().Be(401);
        nextCalled[0].Should().BeFalse();
    }

    [Fact]
    public async Task Returns_401_when_host_does_not_match()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var partnerId = Guid.NewGuid();
        partners.Setup(p => p.GetWithAccountAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner
            {
                PartnerId = partnerId,
                BaseUrl = "https://expected.partner.com",
                Status = PartnerStatus.Active,
                RateLimitPerMin = 100
            });

        var ctx = BuildContext("/api/v1/financial/bank/balance", host: "evil.com",
            headers: new Dictionary<string, string> { ["X-Partner-Id"] = partnerId.ToString() });

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        ctx.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Returns_429_when_rate_limit_exceeded()
    {
        var (mw, partners, cache, enc, nextCalled) = Build();
        var partnerId = Guid.NewGuid();
        partners.Setup(p => p.GetWithAccountAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner
            {
                PartnerId = partnerId,
                BaseUrl = "https://api.partner.com",
                Status = PartnerStatus.Active,
                RateLimitPerMin = 5
            });
        cache.Setup(r => r.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);

        var ctx = BuildContext("/api/v1/financial/bank/balance",
            headers: new Dictionary<string, string> { ["X-Partner-Id"] = partnerId.ToString() });

        await mw.Invoke(ctx, partners.Object, cache.Object, enc.Object);
        ctx.Response.StatusCode.Should().Be(429);
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
}
