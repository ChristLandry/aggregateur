using System.Globalization;
using AggregatorPlatform.Application.Interfaces;
using NCalc;

namespace AggregatorPlatform.Infrastructure.Services;

public class FormulaEvaluator : IFormulaEvaluator
{
    public decimal EvaluateAmount(string formula, IDictionary<string, object?> context)
    {
        var expr = BuildExpression(formula, context);
        var result = expr.Evaluate();
        return Convert.ToDecimal(result, CultureInfo.InvariantCulture);
    }

    public bool EvaluateCondition(string condition, IDictionary<string, object?> context)
    {
        var expr = BuildExpression(condition, context);
        var result = expr.Evaluate();
        return Convert.ToBoolean(result);
    }

    public string EvaluateExpression(string expression, IDictionary<string, object?> context)
    {
        if (context.TryGetValue(expression, out var direct) && direct is not null)
            return direct.ToString() ?? string.Empty;

        var expr = BuildExpression(expression, context);
        var result = expr.Evaluate();
        return result?.ToString() ?? string.Empty;
    }

    private static Expression BuildExpression(string text, IDictionary<string, object?> context)
    {
        var expr = new Expression(text, EvaluateOptions.IgnoreCase);
        foreach (var kvp in context)
        {
            expr.Parameters[kvp.Key] = kvp.Value;
        }
        expr.EvaluateFunction += (name, args) =>
        {
            switch (name.ToUpperInvariant())
            {
                case "IF":
                    args.Result = Convert.ToBoolean(args.Parameters[0].Evaluate())
                        ? args.Parameters[1].Evaluate()
                        : args.Parameters[2].Evaluate();
                    break;
                case "ROUND":
                    {
                        var v = Convert.ToDecimal(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture);
                        var n = Convert.ToInt32(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture);
                        args.Result = Math.Round(v, n);
                    }
                    break;
                case "ABS":
                    args.Result = Math.Abs(Convert.ToDecimal(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture));
                    break;
                case "MIN":
                    args.Result = Math.Min(
                        Convert.ToDecimal(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDecimal(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture));
                    break;
                case "MAX":
                    args.Result = Math.Max(
                        Convert.ToDecimal(args.Parameters[0].Evaluate(), CultureInfo.InvariantCulture),
                        Convert.ToDecimal(args.Parameters[1].Evaluate(), CultureInfo.InvariantCulture));
                    break;
            }
        };
        return expr;
    }
}
