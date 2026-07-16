using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AggregatorPlatform.Application.Features.Subscriptions.Commands;

/// <summary>Requete d'onboarding customer initiee depuis l'ecran de souscription.</summary>
public record OnboardCustomerRequest(
    string BankAccount,
    string PhoneNumber,
    string BankAccountRoot,
    string WalletTemporalyCode,
    Guid PartnerId);

/// <summary>Reponse d'onboarding : ids crees + statut du link wallet.</summary>
public record OnboardCustomerResponse(
    Guid ClientId,
    Guid CustomerId,
    Guid SubscriptionId,
    string? LinkId,
    string Status);

public record OnboardCustomerCommand(OnboardCustomerRequest Request) : IRequest<Result<OnboardCustomerResponse>>;

public class OnboardCustomerValidator : AbstractValidator<OnboardCustomerCommand>
{
    public OnboardCustomerValidator()
    {
        RuleFor(x => x.Request.BankAccount).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Request.BankAccountRoot).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.WalletTemporalyCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.PartnerId).NotEmpty();
    }
}

/// <summary>
/// Handler d'onboarding : appelle bank KYC + wallet KYC (via connector resolver),
/// compare (phoneNumber / dateOfBirth / nationalId), lie le wallet au compte bancaire,
/// puis persiste Client (racine) + Customer + Subscription.
/// </summary>
public class OnboardCustomerCommandHandler : IRequestHandler<OnboardCustomerCommand, Result<OnboardCustomerResponse>>
{
    private readonly IPartnerRepository _partners;
    private readonly ICustomerRepository _customers;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IRepository<Client> _clients;
    private readonly IBankApiClient _bank;
    private readonly IWalletConnectorResolver _walletResolver;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OnboardCustomerCommandHandler> _logger;

    public OnboardCustomerCommandHandler(
        IPartnerRepository partners,
        ICustomerRepository customers,
        ISubscriptionRepository subscriptions,
        IRepository<Client> clients,
        IBankApiClient bank,
        IWalletConnectorResolver walletResolver,
        IUnitOfWork uow,
        ILogger<OnboardCustomerCommandHandler> logger)
    {
        _partners = partners;
        _customers = customers;
        _subscriptions = subscriptions;
        _clients = clients;
        _bank = bank;
        _walletResolver = walletResolver;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<OnboardCustomerResponse>> Handle(OnboardCustomerCommand cmd, CancellationToken ct)
    {
        var req = cmd.Request;

        // 1) Partenaire
        var partner = await _partners.GetByIdAsync(req.PartnerId, ct);
        if (partner is null) return Result<OnboardCustomerResponse>.Failure("PARTNER_NOT_FOUND", "Partner not found.");
        if (partner.Status != PartnerStatus.Active)
            return Result<OnboardCustomerResponse>.Failure("PARTNER_INACTIVE", "Partner is not active.");

        // 2) Bank KYC (via nouvelle signature POST)
        BankKycDto bankKyc;
        try
        {
            // Le connecteur bank_connector n'attend que bankAccount ; les anciens
            // walletTemporalyCode/extras ne sont plus consommes.
            bankKyc = await _bank.GetKycAsync(partner, new BankKycRequest(req.BankAccount), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboard: bank KYC failed for partner {PartnerId} account {Account}", partner.PartnerId, req.BankAccount);
            return Result<OnboardCustomerResponse>.Failure("BANK_KYC_FAILED", ex.Message);
        }

        // 3) Wallet KYC (via connector routing PartnerCode)
        var walletConnector = _walletResolver.Resolve(partner);
        WalletKycDto walletKyc;
        try
        {
            walletKyc = await walletConnector.GetKycAsync(partner,
                new WalletKycRequest(req.PhoneNumber, req.WalletTemporalyCode, null), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboard: wallet KYC failed for partner {PartnerId} phone {Phone}", partner.PartnerId, req.PhoneNumber);
            return Result<OnboardCustomerResponse>.Failure("WALLET_KYC_FAILED", ex.Message);
        }

        // 4) Comparaison bank vs wallet KYC (phone / dob / nationalId)
        var mismatches = new List<string>();
        if (!string.IsNullOrWhiteSpace(bankKyc.PhoneNumber)
            && !string.Equals(bankKyc.PhoneNumber, walletKyc.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            mismatches.Add("phoneNumber");
        if (bankKyc.DateOfBirth.HasValue && bankKyc.DateOfBirth.Value != walletKyc.DateOfBirth)
            mismatches.Add("dateOfBirth");
        if (!string.IsNullOrWhiteSpace(bankKyc.NationalId)
            && !string.Equals(bankKyc.NationalId, walletKyc.NationalId, StringComparison.OrdinalIgnoreCase))
            mismatches.Add("nationalId");
        /*if (mismatches.Count > 0)
            return Result<OnboardCustomerResponse>.Failure("KYC_MISMATCH",
                $"Bank and wallet KYC differ on: {string.Join(", ", mismatches)}.");*/

        // 5) Link wallet <-> compte bancaire
        // extras.activationKey = OTP wallet (requis par le connecteur WAVE).
        var partnerRef = $"ONBOARD-{Guid.NewGuid():N}";
        var linkExtras = new Dictionary<string, object?>
        {
            ["activationKey"] = req.WalletTemporalyCode,
            ["walletTemporalyCode"] = req.WalletTemporalyCode,
        };
        WalletLinkResponse linkResp;
        try
        {
            linkResp = await walletConnector.LinkAsync(partner,
                new WalletLinkRequest(req.PhoneNumber, partnerRef, req.BankAccount, linkExtras), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboard: wallet link failed for partner {PartnerId}", partner.PartnerId);
            return Result<OnboardCustomerResponse>.Failure("WALLET_LINK_FAILED", ex.Message);
        }
        if (!string.Equals(linkResp.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            return Result<OnboardCustomerResponse>.Failure("WALLET_LINK_FAILED",
                linkResp.FailureReason ?? "Wallet link failed.");

        // 6) Verifier unicite (partner, bank, phone) avant persist
        var duplicate = await _subscriptions.ExistsByPartnerBankAndPhoneAsync(
            partner.PartnerId, req.BankAccount, req.PhoneNumber, ct);
        if (duplicate)
            return Result<OnboardCustomerResponse>.Failure("SUBSCRIPTION_DUPLICATE",
                "An active subscription already exists for this partner with the same bank account and phone number.");

        // 7) Client : find-or-create par BankAccountRoot
        var client = await _clients.Query()
            .FirstOrDefaultAsync(c => c.BankAccountRoot == req.BankAccountRoot, ct);
        if (client is null)
        {
            client = new Client
            {
                BankAccountRoot = req.BankAccountRoot,
                FullName = walletKyc.FullName,
                DateOfBirth = walletKyc.DateOfBirth,
                NationalId = walletKyc.NationalId,
                PhoneNumber = walletKyc.PhoneNumber,
            };
            await _clients.AddAsync(client, ct);
        }

        // 8) Customer (par partenaire) rattache au Client
        var customer = new Customer
        {
            ClientId = client.ClientId,
            FullName = walletKyc.FullName,
            DateOfBirth = walletKyc.DateOfBirth,
            NationalId = walletKyc.NationalId,
            Status = CustomerStatus.Active,
            KycStatus = KycStatus.Verified,
        };
        await _customers.AddAsync(customer, ct);

        // 9) Subscription
        var sub = new Subscription
        {
            CustomerId = customer.CustomerId,
            PartnerId = partner.PartnerId,
            BankAccount = req.BankAccount,
            PhoneNumber = req.PhoneNumber,
            PhoneOperator = partner.PartnerCode,
            Status = SubscriptionStatus.Active,
        };
        await _subscriptions.AddAsync(sub, ct);

        await _uow.SaveChangesAsync(ct);

        return Result<OnboardCustomerResponse>.Success(new OnboardCustomerResponse(
            client.ClientId, customer.CustomerId, sub.SubscriptionId, linkResp.LinkId, "SUCCESS"));
    }
}
