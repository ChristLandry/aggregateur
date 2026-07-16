using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Client HTTP du connecteur bancaire externe (projet bank_connector).
/// Aligne strictement le contrat du connecteur : POST /bank/{balance,kyc,transaction,insertmouvement}.
/// Aucune valeur n'est fabriquee cote hub pour combler un champ absent ; seul
/// le fail-safe (exception, circuit ouvert, JSON invalide) produit une reponse
/// neutre pour ne pas propager d'HTTP 500 aux appelants downstream.
/// </summary>
public class BankApiClient : IBankApiClient
{
    public const string HttpClientName = "PartnerBank";

    // Le connecteur bank_connector serialize en camelCase (defaut ASP.NET Core web).
    // On applique la meme politique cote sortant + reception.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _factory;
    private readonly BankConnectorOptions _options;
    private readonly ILogger<BankApiClient> _logger;

    public BankApiClient(
        IHttpClientFactory factory,
        IOptions<BankConnectorOptions> options,
        ILogger<BankApiClient> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    private HttpClient CreateClient(Partner partner)
    {
        var baseUrl =  _options.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException(
                $"Aucune BaseUrl configuree pour le connecteur bank : Partner.BaseUrl vide et BankConnector:BaseUrl non renseigne (partner {partner.PartnerId}).");

        var client = _factory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }

    /// <summary>
    /// POST /bank/balance body { bankAccount } -> { fondDispo }.
    /// Le connecteur ne renvoie que le fond disponible ; la reponse hub reflete
    /// exactement ce contrat. En cas de defaillance (HTTP KO, JSON invalide,
    /// circuit ouvert), on renvoie FondDispo=0 en fail-safe.
    /// </summary>
    public async Task<BankBalanceResponse> GetBalanceAsync(Partner partner, string bankAccount, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/balance", new { bankAccount }, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<BankBalanceResponse>(JsonOptions, cancellationToken);
            return body ?? new BankBalanceResponse(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank balance failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new BankBalanceResponse(0);
        }
    }

    /// <summary>
    /// POST /bank/kyc body { bankAccount } -> { fullName, dateOfBirth, id_Type, nationalId, phoneNumber }.
    /// dateOfBirth est parse depuis "yyyy-MM-dd" ; si le parse echoue on laisse null
    /// (pas de valeur de secours inventee). id_Type est reconnu via [JsonPropertyName]
    /// sur <see cref="ConnectorKycResponse.IdType"/>.
    /// </summary>
    public async Task<BankKycDto> GetKycAsync(Partner partner, BankKycRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/kyc", new { bankAccount = request.BankAccount }, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ConnectorKycResponse>(JsonOptions, cancellationToken);
            if (body is null)
                return new BankKycDto(string.Empty, string.Empty, null, null, null);

            DateOnly? dob = null;
            if (!string.IsNullOrWhiteSpace(body.DateOfBirth))
            {
                if (DateOnly.TryParseExact(body.DateOfBirth, "yyyy-MM-dd", out var parsed))
                    dob = parsed;
                else
                    _logger.LogWarning("Bank KYC: dateOfBirth '{Value}' non parsable (yyyy-MM-dd) pour partner {PartnerId}.",
                        body.DateOfBirth, partner.PartnerId);
            }

            return new BankKycDto(
                PhoneNumber: body.PhoneNumber ?? string.Empty,
                FullName: body.FullName ?? string.Empty,
                DateOfBirth: dob,
                IdType: body.IdType,
                NationalId: body.NationalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank KYC failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new BankKycDto(string.Empty, string.Empty, null, null, null);
        }
    }

    /// <summary>
    /// POST /bank/transaction body { bankAccount, codOpsc, amount, fees, transactionId } ->
    /// { success, transactionBankIdentifier, transactionDate }.
    /// success=true  -> BankTransactionResponse(true,  identifier, date, null)
    /// success=false -> BankTransactionResponse(false, identifier, date, null)
    /// exception/HTTP KO -> (false, "", DateTime.UtcNow, "message technique") -- fail-safe.
    /// </summary>
    public async Task<BankTransactionResponse> TransactionAsync(Partner partner, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/transaction", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(JsonOptions, cancellationToken);
            return body ?? new BankTransactionResponse(false, string.Empty, DateTime.UtcNow, "Empty response body from bank connector.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank transaction failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new BankTransactionResponse(false, string.Empty, DateTime.UtcNow, ex.Message);
        }
    }

    /// <summary>
    /// POST /bank/insertmouvement body { mouvements: [ ... ] } ->
    /// meme shape que /bank/transaction. Le connecteur exige au moins 1 ligne
    /// ([MinLength(1)]) ; on court-circuite en fail-safe si la liste est vide.
    /// </summary>
    public async Task<BankTransactionResponse> InsertMouvementAsync(Partner partner, IReadOnlyList<BankMouvementLine> mouvements, CancellationToken cancellationToken = default)
    {
        if (mouvements is null || mouvements.Count == 0)
        {
            return new BankTransactionResponse(false, string.Empty, DateTime.UtcNow,
                "InsertMouvement requires at least one line (bank_connector [MinLength(1)]).");
        }

        try
        {
            var client = CreateClient(partner);
            var response = await client.PostAsJsonAsync("/bank/insertmouvement", new { mouvements }, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var sentJson = JsonSerializer.Serialize(new { mouvements }, JsonOptions);
                _logger.LogError("Bank insertmouvement HTTP {Status} for partner {PartnerId}. Sent={Sent}. Response={Body}",
                    (int)response.StatusCode, partner.PartnerId, sentJson, errBody);
                return new BankTransactionResponse(false, string.Empty, DateTime.UtcNow, $"Bank connector {(int)response.StatusCode}: {errBody}");
            }
            var body = await response.Content.ReadFromJsonAsync<BankTransactionResponse>(JsonOptions, cancellationToken);
            return body ?? new BankTransactionResponse(false, string.Empty, DateTime.UtcNow, "Empty response body from bank connector.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bank insertmouvement failed for partner {PartnerId} ({Type}: {Message})",
                partner.PartnerId, ex.GetType().Name, ex.Message);
            return new BankTransactionResponse(false, string.Empty, DateTime.UtcNow, ex.Message);
        }
    }

    // ------------------------------------------------------------------
    // DTOs internes de deserialisation
    // ------------------------------------------------------------------

    /// <summary>
    /// Miroir prive de la KycResponse du connecteur. Une classe locale evite de
    /// polluer l'API publique du hub avec la particularite JSON "id_Type" (T maj).
    /// </summary>
    private sealed class ConnectorKycResponse
    {
        public string? FullName { get; set; }
        public string? DateOfBirth { get; set; }
        [JsonPropertyName("id_Type")] public string? IdType { get; set; }
        public string? NationalId { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
