using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AggregatorPlatform.API.Logging;

public class PiiMaskingEnricher : ILogEventEnricher
{
    private static readonly string[] SensitiveProperties = { "PhoneNumber", "BankAccountNumber", "NationalId", "Password", "ApiKey" };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var key in SensitiveProperties)
        {
            if (logEvent.Properties.ContainsKey(key))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(key, "***MASKED***"));
            }
        }
    }
}
