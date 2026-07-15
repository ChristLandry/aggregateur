using AggregatorPlatform.Domain.Entities;

namespace AggregatorPlatform.Application.Interfaces;

public interface ICurrentPartnerService
{
    Guid? PartnerId { get; }
    Partner? Current { get; }
}
