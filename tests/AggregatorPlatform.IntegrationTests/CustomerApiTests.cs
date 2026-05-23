using System.Net;
using FluentAssertions;
using Xunit;

namespace AggregatorPlatform.IntegrationTests;

public class CustomerApiTests : IClassFixture<AggregatorWebAppFactory>
{
    private readonly AggregatorWebAppFactory _factory;

    public CustomerApiTests(AggregatorWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateCustomer_without_partner_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/api/v1/customers",
            new StringContent("{\"fullName\":\"John Doe\",\"dateOfBirth\":\"1990-01-01\"}",
                System.Text.Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
