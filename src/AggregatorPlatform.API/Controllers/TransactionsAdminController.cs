using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Accounting.Queries;
using AggregatorPlatform.Application.Features.Financial.Queries;
using AggregatorPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorPlatform.API.Controllers;

/// <summary>
/// Recherche et consultation des transactions / mouvements en mode admin.
/// Routes sous /api/v1/financial/transactions : exemptees du middleware
/// PartnerAuth (pas de header X-Partner-ApiKey requis), authentification JWT
/// + role Admin/SuperAdmin/Finance.
/// </summary>
[Route("api/v1/financial/transactions")]
[Authorize(Roles = "Admin,SuperAdmin,Finance")]
public class TransactionsAdminController : BaseApiController
{
    /// <summary>
    /// Recherche paginee de transactions. Tous les parametres sont optionnels.
    /// </summary>
    /// <param name="fromDate">Borne basse sur InitiatedAt (ISO-8601).</param>
    /// <param name="toDate">Borne haute sur InitiatedAt (ISO-8601).</param>
    /// <param name="partnerId">Filtre par partenaire.</param>
    /// <param name="status">Filtre par statut (0 Pending, 1 Success, 2 Failed, 3 Cancelled, 4 Reversed).</param>
    /// <param name="bankAccount">Filtre exact sur le numero de compte (compare au ciphertext deterministe).</param>
    /// <param name="phoneNumber">Filtre exact sur le numero de telephone.</param>
    /// <param name="partnerTransactionRef">Recherche partielle sur la reference partenaire (Contains).</param>
    /// <param name="type">Filtre par TransactionType (0..4).</param>
    /// <param name="page">Numero de page (defaut 1).</param>
    /// <param name="pageSize">Taille de page (defaut 50).</param>
    /// <param name="ct">Token d'annulation.</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<TransactionDto>>>> Search(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? partnerId,
        [FromQuery] TransactionStatus? status,
        [FromQuery] string? bankAccount,
        [FromQuery] string? phoneNumber,
        [FromQuery] string? partnerTransactionRef,
        [FromQuery] TransactionType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetTransactionsQuery(
            partnerId, bankAccount, phoneNumber, partnerTransactionRef,
            status, type, fromDate, toDate, page, pageSize);
        return ToResponse(await Mediator.Send(query, ct));
    }

    /// <summary>Detail d'une transaction.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> GetById(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetTransactionByIdQuery(id), ct));

    /// <summary>Liste tous les mouvements comptables generes par une transaction (ordre LineOrder croissant).</summary>
    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MovementDto>>>> GetMovements(Guid id, CancellationToken ct)
        => ToResponse(await Mediator.Send(new GetMovementsByTransactionQuery(id), ct));
}
