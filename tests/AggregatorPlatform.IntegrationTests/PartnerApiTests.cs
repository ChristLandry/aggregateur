using System.Net;
using FluentAssertions;
using Xunit;

namespace AggregatorPlatform.IntegrationTests;

public class PartnerApiTests : IClassFixture<AggregatorWebAppFactory>
{
    private readonly AggregatorWebAppFactory _factory;

    public PartnerApiTests(AggregatorWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAllPartners_without_jwt_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/partners");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
