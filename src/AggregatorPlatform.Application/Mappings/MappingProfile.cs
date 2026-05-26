using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Domain.Entities;
using AutoMapper;

namespace AggregatorPlatform.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Partner, PartnerDto>();
        CreateMap<PartnerAccount, PartnerAccountDto>();
        CreateMap<Customer, CustomerDto>();
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<AccountingSchema, AccountingSchemaDto>();
        CreateMap<AccountingSchemaLine, AccountingSchemaLineDto>();
        CreateMap<Movement, MovementDto>();
        CreateMap<PartnerAccountMovement, PartnerStatementItemDto>();
    }
}
