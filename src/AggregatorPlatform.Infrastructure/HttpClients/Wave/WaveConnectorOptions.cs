namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Options du client HTTP qui parle à la façade Aggregator.WaveConnector.Api.
/// Bind sur la section <c>WaveConnector</c> de <c>appsettings.json</c>.
/// L'URL de base n'est PAS ici : elle est prise sur <c>Partner.BaseUrl</c> à chaque appel.
/// </summary>
public class WaveConnectorOptions
{
    public const string SectionName = "WaveConnector";

    /// <summary>
    /// Valeur du header <c>X-Partner-Id</c> exigé par la façade Wave sur /api/wave/*.
    /// Doit être identique à <c>InboundPartnerId:PartnerId</c> du WaveConnector.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
