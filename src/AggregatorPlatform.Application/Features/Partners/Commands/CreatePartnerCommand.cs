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
        // Format (NotEmpty + MaxLength) ici ; la verification metier d'appartenance
        // a l'enum AllowedPartnerCode est faite dans le handler pour produire un
        // code d'erreur top-level explicite (PARTNER_CODE_NOT_ALLOWED).
        RuleFor(x => x.Request.PartnerCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.BaseUrl).NotEmpty().Must(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Request.Currency).NotEmpty().Length(3);
        // PartnerBankAccount est OPTIONNEL : valide uniquement la longueur si fourni.
        When(x => !string.IsNullOrEmpty(x.Request.PartnerBankAccount), () =>
        {
            RuleFor(x => x.Request.PartnerBankAccount!).MaximumLength(64);
        });

        When(x => !string.IsNullOrEmpty(x.Request.ContactEmail), () =>
        {
            RuleFor(x => x.Request.ContactEmail!).EmailAddress().MaximumLength(200);
        });

        When(x => !string.IsNullOrEmpty(x.Request.ContactPhone), () =>
        {
            RuleFor(x => x.Request.ContactPhone!).MaximumLength(30);
        });

        When(x => x.Request.LowBalanceThresholdPercent.HasValue, () =>
        {
            RuleFor(x => x.Request.LowBalanceThresholdPercent!.Value).InclusiveBetween(1, 100);
        });

        When(x => x.Request.LowBalanceReferenceAmount.HasValue, () =>
        {
            RuleFor(x => x.Request.LowBalanceReferenceAmount!.Value).GreaterThan(0);
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
        // 1. Defense en profondeur : meme check que le validator (cas ou il serait bypass).
        if (!Enum.TryParse<AllowedPartnerCode>(request.Request.PartnerCode, ignoreCase: false, out _))
        {
            return Result<CreatePartnerResponse>.Failure("PARTNER_CODE_NOT_ALLOWED",
                $"PartnerCode '{request.Request.PartnerCode}' is not in the allowed list. " +
                $"Allowed values: {string.Join(", ", Enum.GetNames<AllowedPartnerCode>())}.");
        }

        // 2. Unicite du code.
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
            IpWhitelist = request.Request.IpWhitelist,
            ContactEmail = request.Request.ContactEmail,
            ContactPhone = request.Request.ContactPhone,
            LowBalanceThresholdPercent = request.Request.LowBalanceThresholdPercent,
            LowBalanceReferenceAmount = request.Request.LowBalanceReferenceAmount,
            AlertChannels = request.Request.AlertChannels
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
