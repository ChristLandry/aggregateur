using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.Common.Exceptions;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

public record CreatePartnerCommand(CreatePartnerRequest Request) : IRequest<Result<CreatePartnerResponse>>;

public class CreatePartnerValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerValidator()
    {
        RuleFor(x => x.Request.PartnerCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.BaseUrl).NotEmpty().Must(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Request.RateLimitPerMin).GreaterThan(0);
        // PartnerBankAccount est OPTIONNEL : valide uniquement la longueur si fourni.
        When(x => !string.IsNullOrEmpty(x.Request.PartnerBankAccount), () =>
        {
            RuleFor(x => x.Request.PartnerBankAccount!).MaximumLength(64);
        });
    }
}

public class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, Result<CreatePartnerResponse>>
{
    private readonly IPartnerRepository _partners;
    private readonly IPartnerAccountRepository _accounts;
    private readonly IUnitOfWork _uow;
    private readonly IEncryptionService _encryption;

    public CreatePartnerCommandHandler(IPartnerRepository partners, IPartnerAccountRepository accounts,
        IUnitOfWork uow, IEncryptionService encryption)
    {
        _partners = partners;
        _accounts = accounts;
        _uow = uow;
        _encryption = encryption;
    }

    public async Task<Result<CreatePartnerResponse>> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _partners.GetByCodeAsync(request.Request.PartnerCode, cancellationToken);
        if (existing is not null)
            return Result<CreatePartnerResponse>.Failure("PARTNER_CODE_EXISTS", "Partner code already exists.");

        var apiKey = _encryption.GenerateApiKey();
        var partner = new Partner
        {
            PartnerCode = request.Request.PartnerCode,
            Name = request.Request.Name,
            BaseUrl = request.Request.BaseUrl,
            ApiKey = _encryption.ComputeSha256(apiKey),
            AccountCode = request.Request.AccountCode,
            Status = PartnerStatus.Inactive,
            Currency = request.Request.Currency,
            WebhookUrl = request.Request.WebhookUrl,
            RateLimitPerMin = request.Request.RateLimitPerMin,
            IpWhitelist = request.Request.IpWhitelist,
            RequireHmac = request.Request.RequireHmac
        };
        await _partners.AddAsync(partner, cancellationToken);

        var account = new PartnerAccount
        {
            PartnerId = partner.PartnerId,
            PartnerBankAccount = request.Request.PartnerBankAccount ?? string.Empty,
            Balance = 0,
            Currency = request.Request.Currency
        };
        await _accounts.AddAsync(account, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result<CreatePartnerResponse>.Success(new CreatePartnerResponse(partner.PartnerId, partner.PartnerCode, apiKey));
    }
}
