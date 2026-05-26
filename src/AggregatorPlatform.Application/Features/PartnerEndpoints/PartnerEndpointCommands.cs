using AggregatorPlatform.Application.Common;
using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace AggregatorPlatform.Application.Features.PartnerEndpoints;

// --------------------------- CREATE ---------------------------
public record CreatePartnerEndpointCommand(CreatePartnerEndpointRequest Request)
    : IRequest<Result<Guid>>;

public class CreatePartnerEndpointValidator : AbstractValidator<CreatePartnerEndpointCommand>
{
    public CreatePartnerEndpointValidator()
    {
        RuleFor(x => x.Request.PartnerId).NotEmpty();
        RuleFor(x => x.Request.EndpointKey).IsInEnum();
    }
}

public class CreatePartnerEndpointCommandHandler
    : IRequestHandler<CreatePartnerEndpointCommand, Result<Guid>>
{
    private readonly IPartnerEndpointRepository _repo;
    private readonly IPartnerRepository _partners;
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IUnitOfWork _uow;

    public CreatePartnerEndpointCommandHandler(
        IPartnerEndpointRepository repo, IPartnerRepository partners,
        IAccountingSchemaRepository schemas, IUnitOfWork uow)
    {
        _repo = repo; _partners = partners; _schemas = schemas; _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreatePartnerEndpointCommand request, CancellationToken cancellationToken)
    {
        var partner = await _partners.GetByIdAsync(request.Request.PartnerId, cancellationToken);
        if (partner is null)
            return Result<Guid>.Failure("PARTNER_NOT_FOUND", "Partner not found.");

        // Verifie le doublon (Partner, EndpointKey).
        var existing = await _repo.GetByPartnerAndKeyAsync(request.Request.PartnerId, request.Request.EndpointKey, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("PARTNER_ENDPOINT_EXISTS",
                $"A link already exists for partner {request.Request.PartnerId} and endpoint {request.Request.EndpointKey}.");

        // Verifie le schema si fourni.
        if (request.Request.SchemaId.HasValue)
        {
            var schema = await _schemas.GetByIdAsync(request.Request.SchemaId.Value, cancellationToken);
            if (schema is null)
                return Result<Guid>.Failure("SCHEMA_NOT_FOUND", "Schema not found.");
        }

        var entity = new PartnerEndpoint
        {
            PartnerId = request.Request.PartnerId,
            EndpointKey = request.Request.EndpointKey,
            SchemaId = request.Request.SchemaId,
        };
        await _repo.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.PartnerEndpointId);
    }
}

// --------------------------- DELETE ---------------------------
public record DeletePartnerEndpointCommand(Guid PartnerEndpointId) : IRequest<Result>;

public class DeletePartnerEndpointCommandHandler : IRequestHandler<DeletePartnerEndpointCommand, Result>
{
    private readonly IPartnerEndpointRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeletePartnerEndpointCommandHandler(IPartnerEndpointRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(DeletePartnerEndpointCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.PartnerEndpointId, cancellationToken);
        if (entity is null)
            return Result.Failure("PARTNER_ENDPOINT_NOT_FOUND", "Partner endpoint link not found.");
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// --------------------------- ATTACH SCHEMA ---------------------------
public record AttachSchemaCommand(Guid PartnerEndpointId, AttachSchemaRequest Request)
    : IRequest<Result>;

public class AttachSchemaValidator : AbstractValidator<AttachSchemaCommand>
{
    public AttachSchemaValidator() => RuleFor(x => x.Request.SchemaId).NotEmpty();
}

public class AttachSchemaCommandHandler : IRequestHandler<AttachSchemaCommand, Result>
{
    private readonly IPartnerEndpointRepository _repo;
    private readonly IAccountingSchemaRepository _schemas;
    private readonly IUnitOfWork _uow;

    public AttachSchemaCommandHandler(IPartnerEndpointRepository repo, IAccountingSchemaRepository schemas, IUnitOfWork uow)
    { _repo = repo; _schemas = schemas; _uow = uow; }

    public async Task<Result> Handle(AttachSchemaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.PartnerEndpointId, cancellationToken);
        if (entity is null)
            return Result.Failure("PARTNER_ENDPOINT_NOT_FOUND", "Partner endpoint link not found.");

        var schema = await _schemas.GetByIdAsync(request.Request.SchemaId, cancellationToken);
        if (schema is null)
            return Result.Failure("SCHEMA_NOT_FOUND", "Schema not found.");

        entity.SchemaId = request.Request.SchemaId;
        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

// --------------------------- DETACH SCHEMA ---------------------------
public record DetachSchemaCommand(Guid PartnerEndpointId) : IRequest<Result>;

public class DetachSchemaCommandHandler : IRequestHandler<DetachSchemaCommand, Result>
{
    private readonly IPartnerEndpointRepository _repo;
    private readonly IUnitOfWork _uow;

    public DetachSchemaCommandHandler(IPartnerEndpointRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<Result> Handle(DetachSchemaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.PartnerEndpointId, cancellationToken);
        if (entity is null)
            return Result.Failure("PARTNER_ENDPOINT_NOT_FOUND", "Partner endpoint link not found.");
        if (entity.SchemaId is null)
            return Result.Success(); // deja detache

        entity.SchemaId = null;
        _repo.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
