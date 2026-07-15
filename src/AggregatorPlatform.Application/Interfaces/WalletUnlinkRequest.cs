namespace AggregatorPlatform.Application.Interfaces;

/// <summary>Demande de delaison : identifie par LinkId ou PhoneNumber.</summary>
public record WalletUnlinkRequest(
    string? LinkId = null,
    string? PhoneNumber = null,
    string? PartnerRef = null);
