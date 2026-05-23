using AggregatorPlatform.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace AggregatorPlatform.UnitTests.Infrastructure;

public class FormulaEvaluatorTests
{
    private readonly FormulaEvaluator _eval = new();

    [Fact]
    public void Computes_percentage_of_amount()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["AMOUNT"] = 10000m };
        _eval.EvaluateAmount("AMOUNT * 0.015", ctx).Should().Be(150m);
    }

    [Fact]
    public void Supports_IF_function()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["AMOUNT"] = 600000m };
        _eval.EvaluateAmount("IF(AMOUNT > 500000, AMOUNT * 0.01, AMOUNT * 0.02)", ctx).Should().Be(6000m);
    }

    [Fact]
    public void Supports_MAX_and_ROUND()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["AMOUNT"] = 1234m };
        _eval.EvaluateAmount("MAX(ROUND(AMOUNT * 0.015, 0), 500)", ctx).Should().Be(500m);
    }

    [Fact]
    public void Supports_ABS_and_MIN()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _eval.EvaluateAmount("ABS(-50)", ctx).Should().Be(50m);
        _eval.EvaluateAmount("MIN(10, 5)", ctx).Should().Be(5m);
    }

    [Fact]
    public void Evaluates_conditions()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AMOUNT"] = 1000m,
            ["TX.Currency"] = "XOF"
        };
        _eval.EvaluateCondition("AMOUNT > 0 AND [TX.Currency] = 'XOF'", ctx).Should().BeTrue();
    }

    [Fact]
    public void Resolves_partner_account_code_via_expression()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["PARTNER.AccountCode"] = "P-001"
        };
        _eval.EvaluateExpression("PARTNER.AccountCode", ctx).Should().Be("P-001");
    }
}
