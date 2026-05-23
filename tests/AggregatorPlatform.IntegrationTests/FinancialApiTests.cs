using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Xunit;

namespace AggregatorPlatform.IntegrationTests;

public class FinancialApiTests : IClassFixture<AggregatorWebAppFactory>
{
    private readonly AggregatorWebAppFactory _factory;

    public FinancialApiTests(AggregatorWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task BankDebit_without_partner_header_returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/api/v1/financial/bank/debit",
            new StringContent("{\"partnerTransactionRef\":\"R1\",\"subscriptionId\":\"00000000-0000-0000-0000-000000000000\",\"amount\":100,\"currency\":\"XOF\"}", System.Text.Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_endpoint_is_reachable()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health");
        // Health may return 503 if DB unavailable in InMemory test mode; we just ensure routed.
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}
