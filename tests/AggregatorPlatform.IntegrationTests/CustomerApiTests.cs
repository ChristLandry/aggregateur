using System.Net;
using FluentAssertions;
using Xunit;

namespace AggregatorPlatform.IntegrationTests;

public class CustomerApiTests : IClassFixture<AggregatorWebAppFactory>
{
    private readonly AggregatorWebAppFactory _factory;

    public CustomerApiTests(AggregatorWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task ListCustomers_returns_200_with_envelope()
    {
        var client = _factory.CreateAuthenticatedClient();
        var resp = await client.GetAsync("/api/v1/customers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("\"success\":true");
    }

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
