namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Levée quand la façade renvoie un code HTTP ≥ 400. Un <c>success:false</c>
/// (200 OK côté HTTP) reste un cas métier et n'est PAS traduit en exception.
/// </summary>
public class WaveConnectorException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public WaveConnectorException(string message, int statusCode, string? responseBody = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
