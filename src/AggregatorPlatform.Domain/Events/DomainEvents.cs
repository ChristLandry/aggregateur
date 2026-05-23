using AggregatorPlatform.Domain.Common;

namespace AggregatorPlatform.Domain.Events;

public record TransactionCreatedEvent(Guid TransactionId, Guid PartnerId) : DomainEvent;

public record TransactionCompletedEvent(Guid TransactionId, Guid PartnerId, bool Success) : DomainEvent;

public record PartnerStatusChangedEvent(Guid PartnerId, string OldStatus, string NewStatus) : DomainEvent;

public record SubscriptionCreatedEvent(Guid SubscriptionId, Guid CustomerId, Guid PartnerId) : DomainEvent;

public record AccountingAppliedEvent(Guid TransactionId, Guid SchemaId) : DomainEvent;
