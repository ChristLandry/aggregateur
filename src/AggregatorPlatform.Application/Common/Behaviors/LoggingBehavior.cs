using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AggregatorPlatform.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[CQRS] Handling {RequestName}", requestName);
        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("[CQRS] Handled {RequestName} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[CQRS] {RequestName} failed after {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
