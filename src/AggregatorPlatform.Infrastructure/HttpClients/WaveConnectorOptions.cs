namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Options du client HTTP qui parle à la façade Aggregator.WaveConnector.Api.
/// Bind sur la section <c>WaveConnector</c> de <c>appsettings.json</c>.
/// </summary>
public class WaveConnectorOptions
{
    public const string SectionName = "WaveConnector";

    /// <summary>URL de base de la façade Wave (ex. https://localhost:44320).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Clé API entrante de la façade (header <c>Api-Key</c>).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
