namespace AggregatorPlatform.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"Entity '{entity}' with key '{key}' was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }
    public BusinessRuleException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class DuplicateException : Exception
{
    public DuplicateException(string message) : base(message) { }
}
