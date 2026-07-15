namespace AggregatorPlatform.Application.Interfaces;

public interface IFormulaEvaluator
{
    decimal EvaluateAmount(string formula, IDictionary<string, object?> context);
    bool EvaluateCondition(string condition, IDictionary<string, object?> context);
    string EvaluateExpression(string expression, IDictionary<string, object?> context);
}
