using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

/// <summary>
/// Selectionne le connecteur wallet a utiliser pour un partenaire donne
/// (route par <see cref="Partner.PartnerCode"/>).
/// </summary>
public interface IWalletConnectorResolver
{
    IWalletApiClient Resolve(Partner partner);
}
