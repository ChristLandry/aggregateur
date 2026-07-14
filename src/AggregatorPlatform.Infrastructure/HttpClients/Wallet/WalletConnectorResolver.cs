using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Route un partenaire vers le connecteur wallet approprie a partir de
/// <see cref="Partner.PartnerCode"/> :
/// <list type="bullet">
///   <item>WAVE -> <see cref="WaveLinkedAccountConnector"/></item>
///   <item>autre -> <see cref="WalletApiClient"/> (connecteur generique par defaut)</item>
/// </list>
/// </summary>
public class WalletConnectorResolver : IWalletConnectorResolver
{
    private readonly WalletApiClient _generic;
    private readonly WaveLinkedAccountConnector _wave;
    private readonly ILogger<WalletConnectorResolver> _logger;

    public WalletConnectorResolver(
        WalletApiClient generic,
        WaveLinkedAccountConnector wave,
        ILogger<WalletConnectorResolver> logger)
    {
        _generic = generic;
        _wave = wave;
        _logger = logger;
    }

    public IWalletApiClient Resolve(Partner partner)
    {
        var code = partner.PartnerCode?.Trim().ToUpperInvariant();
        IWalletApiClient chosen = code switch
        {
            "WAVE" => _wave,
            _ => _generic
        };
        _logger.LogDebug("WalletConnectorResolver: {PartnerCode} -> {Connector}",
            code, chosen.GetType().Name);
        return chosen;
    }
}
