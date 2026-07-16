using AggregatorPlatform.Application.DTOs;
using AggregatorPlatform.Application.Features.Financial.Commands;
using AggregatorPlatform.Application.Interfaces;
using AggregatorPlatform.Application.Mappings;
using AggregatorPlatform.Domain.Entities;
using AggregatorPlatform.Domain.Enums;
using AggregatorPlatform.Domain.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AggregatorPlatform.UnitTests.Application;

public class BankDebitCommandHandlerTests
{
    private static IMapper BuildMapper()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        return cfg.CreateMapper();
    }

    private static BankDebitCommandHandler BuildHandler(
        out Mock<ITransactionRepository> txs,
        out Mock<ISubscriptionRepository> subs,
        out Mock<IPartnerRepository> partners,
        out Mock<IBankApiClient> bank,
        out Mock<IAccountingEngine> accounting)
    {
        txs = new Mock<ITransactionRepository>();
        subs = new Mock<ISubscriptionRepository>();
        partners = new Mock<IPartnerRepository>();
        bank = new Mock<IBankApiClient>();
        accounting = new Mock<IAccountingEngine>();
        var uow = new Mock<IUnitOfWork>();
        var webhooks = new Mock<IWebhookService>();
        var partnerEndpoints = new Mock<IPartnerEndpointRepository>();
        var schemaId = Guid.NewGuid();
        partnerEndpoints
            .Setup(p => p.GetByPartnerAndKeyAsync(It.IsAny<Guid>(), It.IsAny<FinancialEndpointKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid pid, FinancialEndpointKey k, CancellationToken _) =>
                new PartnerEndpoint { PartnerId = pid, EndpointKey = k, SchemaId = schemaId });
        var schemas = new Mock<IAccountingSchemaRepository>();
        // Par defaut : schema bank-managed (skip AccountingEngine dans le pipeline).
        schemas.Setup(s => s.GetByIdAsync(schemaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingSchema { SchemaId = schemaId, Name = "TEST", IsBankManaged = true });
        // Balance UNKNOWN -> balance check tolere en dev (pas de blocage).
        bank.Setup(b => b.GetBalanceAsync(It.IsAny<Partner>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankBalanceResponse("", 0, "XOF", "UNKNOWN"));
        return new BankDebitCommandHandler(txs.Object, subs.Object, partners.Object, partnerEndpoints.Object,
            schemas.Object, bank.Object,
            uow.Object, accounting.Object, webhooks.Object, BuildMapper(),
            NullLogger<BankDebitCommandHandler>.Instance);
    }

    private static BankTransactionInitiateRequest MakeReq(string @ref = "REF", string phone = "+22177", string bank = "ACC123") =>
        new()
        {
            PartnerTransactionRef = @ref,
            PhoneNumber = phone,
            BankAccount = bank,
            Amount = 1000,
            Currency = "XOF",
            OperationType = "BTW",
        };

    [Fact]
    public async Task Fails_when_partner_transaction_ref_already_exists()
    {
        var handler = BuildHandler(out var txs, out _, out var partners, out _, out _);
        var partnerId = Guid.NewGuid();
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Active, ApiKey = "hashed-key" });
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "ABC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Transaction
            {
                TransactionId = Guid.NewGuid(),
                PartnerId = partnerId,
                PartnerTransactionRef = "ABC",
                Amount = 1000,
                Currency = "XOF",
                Status = TransactionStatus.Success,
                TransactionType = TransactionType.BankDebit
            });

        var result = await handler.Handle(new BankDebitCommand(partnerId, MakeReq(@ref: "ABC")), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("DUPLICATE_PARTNER_TRANSACTION_REF");
    }

    [Fact]
    public async Task Fails_when_partner_inactive()
    {
        var handler = BuildHandler(out _, out _, out var partners, out _, out _);
        var partnerId = Guid.NewGuid();
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Inactive, ApiKey = "hashed-key" });

        var result = await handler.Handle(new BankDebitCommand(partnerId, MakeReq(@ref: "REF-X")), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("PARTNER_INACTIVE");
    }

    [Fact]
    public async Task Fails_when_subscription_not_found_by_phone_and_bank()
    {
        var handler = BuildHandler(out var txs, out var subs, out var partners, out _, out _);
        var partnerId = Guid.NewGuid();
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "REF1", It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Active, ApiKey = "hashed-key" });
        subs.Setup(x => x.GetActiveSubscriptionByPartnerAndContactAsync(partnerId, "+22177", "ACC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await handler.Handle(new BankDebitCommand(partnerId, MakeReq(@ref: "REF1")), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("SUBSCRIPTION_NOT_FOUND");
    }

    [Fact]
    public async Task Fails_transaction_when_bank_returns_error()
    {
        var handler = BuildHandler(out var txs, out var subs, out var partners, out var bank, out _);
        var partnerId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "REF2", It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Active, ApiKey = "hashed-key" });
        subs.Setup(x => x.GetActiveSubscriptionByPartnerAndContactAsync(partnerId, "+22177", "ACC123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subscription
            {
                SubscriptionId = subscriptionId,
                PartnerId = partnerId,
                CustomerId = Guid.NewGuid(),
                BankAccount = "ACC123",
                PhoneNumber = "+22177",
                Status = SubscriptionStatus.Active
            });
        bank.Setup(x => x.DebitAsync(It.IsAny<Partner>(), It.IsAny<BankTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankTransactionResponse("EXT-1", "FAILED", "INSUFFICIENT_FUNDS"));

        var result = await handler.Handle(new BankDebitCommand(partnerId, MakeReq(@ref: "REF2")), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("TRANSACTION_FAILED");
    }
}
