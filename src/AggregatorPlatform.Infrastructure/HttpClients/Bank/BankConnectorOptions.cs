namespace AggregatorPlatform.Infrastructure.HttpClients;

/// <summary>
/// Options bind sur la section BankConnector des appsettings.json.
/// Sert de FALLBACK uniquement : la BaseUrl du connecteur bancaire est lue
/// en priorite depuis Partner.BaseUrl (chaque partenaire pointe sur sa propre
/// instance de bank_connector). Cette valeur n'est utilisee que si le partner
/// n'a pas de BaseUrl configuree.
/// </summary>
public class BankConnectorOptions
{
    public const string SectionName = "BankConnector";

    public string? BaseUrl { get; set; }
}
