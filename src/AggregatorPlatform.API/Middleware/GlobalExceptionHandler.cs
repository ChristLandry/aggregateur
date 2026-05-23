using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.Common.Exceptions;
using FluentValidation;

namespace AggregatorPlatform.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(vex, "Validation failed.");
            var msg = string.Join("; ", vex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            await WriteAsync(context, 400, "VALIDATION_ERROR", msg);
        }
        catch (NotFoundException nf)
        {
            _logger.LogInformation(nf, "Resource not found.");
            await WriteAsync(context, 404, "NOT_FOUND", nf.Message);
        }
        catch (BusinessRuleException bre)
        {
            _logger.LogWarning(bre, "Business rule violation.");
            await WriteAsync(context, 422, bre.ErrorCode, bre.Message);
        }
        catch (UnauthorizedAccessException ua)
        {
            _logger.LogWarning(ua, "Unauthorized.");
            await WriteAsync(context, 401, "UNAUTHORIZED", "Unauthorized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception. RequestId={RequestId}", context.TraceIdentifier);
            var detail = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            await WriteAsync(context, 500, "INTERNAL_ERROR", detail);
        }
    }

    private static Task WriteAsync(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(ApiResponse.Fail(code, message));
    }
}
