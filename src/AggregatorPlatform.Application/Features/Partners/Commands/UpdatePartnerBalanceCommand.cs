using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.Partners.Commands;

/// <summary>
/// Met a jour le solde d'un partenaire. Cree automatiquement un mouvement
/// PartnerAccountMovement (ajustement manuel) pour conserver l'audit du delta.
/// </summary>
public record UpdatePartnerBalanceCommand(Guid PartnerId, UpdatePartnerBalanceRequest Request)
    : IRequest<Result<PartnerBalanceDto>>;

public class UpdatePartnerBalanceValidator : AbstractValidator<UpdatePartnerBalanceCommand>
{
    public UpdatePartnerBalanceValidator()
    {
        RuleFor(x => x.Request.Balance).GreaterThanOrEqualTo(0);
    }
}

public class UpdatePartnerBalanceCommandHandler
    : IRequestHandler<UpdatePartnerBalanceCommand, Result<PartnerBalanceDto>>
{
    private readonly IPartnerAccountRepository _accounts;
    private readonly IRepository<PartnerAccountMovement> _movements;
    private readonly IUnitOfWork _uow;

    public UpdatePartnerBalanceCommandHandler(
        IPartnerAccountRepository accounts,
        IRepository<PartnerAccountMovement> movements,
        IUnitOfWork uow)
    {
        _accounts = accounts;
        _movements = movements;
        _uow = uow;
    }

    public async Task<Result<PartnerBalanceDto>> Handle(UpdatePartnerBalanceCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByPartnerIdAsync(request.PartnerId, cancellationToken);
        if (account is null)
            return Result<PartnerBalanceDto>.Failure("ACCOUNT_NOT_FOUND", "Partner account not found.");

        var before = account.Balance;
        var newBalance = request.Request.Balance;
        var delta = newBalance - before;

        // Trace de l'ajustement (audit) — meme si delta est nul on enregistre l'operation manuelle.
        var movement = new PartnerAccountMovement
        {
            PartnerId = request.PartnerId,
            TransactionId = null,
            MovementType = delta >= 0 ? MovementType.Credit : MovementType.Debit,
            Amount = Math.Abs(delta),
            BalanceBefore = before,
            BalanceAfter = newBalance,
            MovementDate = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(request.Request.Reason)
                ? "Manual balance adjustment"
                : request.Request.Reason,
        };
        await _movements.AddAsync(movement, cancellationToken);

        account.Balance = newBalance;
        account.LastMovementAt = DateTime.UtcNow;
        _accounts.Update(account);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result<PartnerBalanceDto>.Success(new PartnerBalanceDto(
            account.PartnerId, account.Balance, account.Currency, account.LastMovementAt));
    }
}
