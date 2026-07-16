using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AggregatorPlatform.Application.Features.Financial.Queries;

public record GetBankBalanceQuery(Guid PartnerId, BankBalanceRequest Request) : IRequest<Result<BankBalanceDto>>;
public record GetWalletBalanceQuery(Guid PartnerId, Guid SubscriptionId) : IRequest<Result<BalanceDto>>;
public record GetBankKycQuery(Guid PartnerId, BankKycRequest Request) : IRequest<Result<BankKycDto>>;
public record GetWalletKycQuery(Guid PartnerId, WalletKycRequest Request) : IRequest<Result<WalletKycDto>>;

public class GetBankBalanceQueryHandler : IRequestHandler<GetBankBalanceQuery, Result<BankBalanceDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ISubscriptionRepository _subs;
    private readonly IBankApiClient _bank;

    public GetBankBalanceQueryHandler(IPartnerRepository partners, ISubscriptionRepository subs,
        IBankApiClient bank)
    {
        _partners = partners;
        _subs = subs;
        _bank = bank;
    }

    public async Task<Result<BankBalanceDto>> Handle(GetBankBalanceQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.BankAccount) || string.IsNullOrWhiteSpace(request.Request.PhoneNumber))
            return Result<BankBalanceDto>.Failure("SUBSCRIPTION_REQUIRED", "BankAccount and PhoneNumber are both required.");

        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<BankBalanceDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var sub = await _subs.GetActiveSubscriptionByPartnerAndContactAsync(
            request.PartnerId, request.Request.PhoneNumber, request.Request.BankAccount, cancellationToken);
        if (sub is null)
            return Result<BankBalanceDto>.Failure("SUBSCRIPTION_NOT_FOUND",
                "No active subscription found for the provided PhoneNumber and BankAccount pair.");

        var resp = await _bank.GetBalanceAsync(partner, sub.BankAccount, cancellationToken);
        return Result<BankBalanceDto>.Success(new BankBalanceDto(sub.BankAccount, resp.FondDispo));
    }
}

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, Result<BalanceDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ISubscriptionRepository _subs;
    private readonly IWalletConnectorResolver _walletResolver;

    public GetWalletBalanceQueryHandler(IPartnerRepository partners, ISubscriptionRepository subs, IWalletConnectorResolver walletResolver)
    {
        _partners = partners;
        _subs = subs;
        _walletResolver = walletResolver;
    }

    public async Task<Result<BalanceDto>> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null || sub.PartnerId != request.PartnerId)
            return Result<BalanceDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found.");
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<BalanceDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var resp = await _walletResolver.Resolve(partner).GetBalanceAsync(partner, sub.PhoneNumber, cancellationToken);
        return Result<BalanceDto>.Success(new BalanceDto(resp.PhoneNumber, resp.Balance, resp.Currency, resp.Status));
    }
}

public class GetBankKycQueryHandler : IRequestHandler<GetBankKycQuery, Result<BankKycDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly IBankApiClient _bank;

    public GetBankKycQueryHandler(IPartnerRepository partners, IBankApiClient bank)
    {
        _partners = partners;
        _bank = bank;
    }

    public async Task<Result<BankKycDto>> Handle(GetBankKycQuery request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<BankKycDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var dto = await _bank.GetKycAsync(partner, request.Request, cancellationToken);
        return Result<BankKycDto>.Success(dto);
    }
}

public class GetWalletKycQueryHandler : IRequestHandler<GetWalletKycQuery, Result<WalletKycDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly IWalletConnectorResolver _walletResolver;

    public GetWalletKycQueryHandler(IPartnerRepository partners, IWalletConnectorResolver walletResolver)
    {
        _partners = partners;
        _walletResolver = walletResolver;
    }

    public async Task<Result<WalletKycDto>> Handle(GetWalletKycQuery request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<WalletKycDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var dto = await _walletResolver.Resolve(partner).GetKycAsync(partner, request.Request, cancellationToken);
        return Result<WalletKycDto>.Success(dto);
    }
}
