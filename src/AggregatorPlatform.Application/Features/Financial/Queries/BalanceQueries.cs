using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Interfaces;
using MediatR;

namespace AggregatorPlatform.Application.Features.Financial.Queries;

public record GetBankBalanceQuery(Guid PartnerId, Guid SubscriptionId) : IRequest<Result<BalanceDto>>;
public record GetWalletBalanceQuery(Guid PartnerId, Guid SubscriptionId) : IRequest<Result<BalanceDto>>;
public record GetBankKycQuery(Guid PartnerId, Guid SubscriptionId) : IRequest<Result<KycDto>>;
public record GetWalletKycQuery(Guid PartnerId, Guid SubscriptionId) : IRequest<Result<KycDto>>;

public class GetBankBalanceQueryHandler : IRequestHandler<GetBankBalanceQuery, Result<BalanceDto>>
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

    public async Task<Result<BalanceDto>> Handle(GetBankBalanceQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null || sub.PartnerId != request.PartnerId)
            return Result<BalanceDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found.");

        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<BalanceDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var resp = await _bank.GetBalanceAsync(partner, sub.BankAccountNumber, cancellationToken);
        return Result<BalanceDto>.Success(new BalanceDto(resp.AccountNumber, resp.Balance, resp.Currency, resp.Status));
    }
}

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, Result<BalanceDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ISubscriptionRepository _subs;
    private readonly IWalletApiClient _wallet;

    public GetWalletBalanceQueryHandler(IPartnerRepository partners, ISubscriptionRepository subs, IWalletApiClient wallet)
    {
        _partners = partners;
        _subs = subs;
        _wallet = wallet;
    }

    public async Task<Result<BalanceDto>> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null || sub.PartnerId != request.PartnerId)
            return Result<BalanceDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found.");
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<BalanceDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var resp = await _wallet.GetBalanceAsync(partner, sub.PhoneNumber, cancellationToken);
        return Result<BalanceDto>.Success(new BalanceDto(resp.PhoneNumber, resp.Balance, resp.Currency, resp.Status));
    }
}

public class GetBankKycQueryHandler : IRequestHandler<GetBankKycQuery, Result<KycDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ISubscriptionRepository _subs;
    private readonly IBankApiClient _bank;

    public GetBankKycQueryHandler(IPartnerRepository partners, ISubscriptionRepository subs, IBankApiClient bank)
    {
        _partners = partners;
        _subs = subs;
        _bank = bank;
    }

    public async Task<Result<KycDto>> Handle(GetBankKycQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null || sub.PartnerId != request.PartnerId)
            return Result<KycDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found.");
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<KycDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var resp = await _bank.GetKycAsync(partner, sub.BankAccountNumber, cancellationToken);
        return Result<KycDto>.Success(new KycDto(resp.AccountNumber, resp.FullName, resp.Status, resp.KycLevel));
    }
}

public class GetWalletKycQueryHandler : IRequestHandler<GetWalletKycQuery, Result<KycDto>>
{
    private readonly IPartnerRepository _partners;
    private readonly ISubscriptionRepository _subs;
    private readonly IWalletApiClient _wallet;

    public GetWalletKycQueryHandler(IPartnerRepository partners, ISubscriptionRepository subs, IWalletApiClient wallet)
    {
        _partners = partners;
        _subs = subs;
        _wallet = wallet;
    }

    public async Task<Result<KycDto>> Handle(GetWalletKycQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subs.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub is null || sub.PartnerId != request.PartnerId)
            return Result<KycDto>.Failure("SUBSCRIPTION_INVALID", "Subscription not found.");
        var partner = await _partners.GetByIdAsync(request.PartnerId, cancellationToken);
        if (partner is null) return Result<KycDto>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        var resp = await _wallet.GetKycAsync(partner, sub.PhoneNumber, cancellationToken);
        return Result<KycDto>.Success(new KycDto(resp.PhoneNumber, resp.FullName, resp.Status, resp.KycLevel));
    }
}
