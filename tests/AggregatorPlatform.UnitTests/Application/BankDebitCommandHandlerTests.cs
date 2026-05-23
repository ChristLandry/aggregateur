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
        var feeCalc = new Mock<IFeeCalculator>();
        feeCalc.Setup(f => f.CalculateAsync(It.IsAny<Guid>(), It.IsAny<TransactionType>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);
        var webhooks = new Mock<IWebhookService>();
        return new BankDebitCommandHandler(txs.Object, subs.Object, partners.Object, uow.Object,
            feeCalc.Object, accounting.Object, webhooks.Object, BuildMapper(),
            NullLogger<BankDebitCommandHandler>.Instance, bank.Object);
    }

    [Fact]
    public async Task Returns_existing_transaction_when_idempotent_hit()
    {
        var handler = BuildHandler(out var txs, out _, out _, out _, out _);
        var partnerId = Guid.NewGuid();
        var existing = new Transaction
        {
            TransactionId = Guid.NewGuid(),
            PartnerId = partnerId,
            PartnerTransactionRef = "ABC",
            Amount = 1000,
            NetAmount = 950,
            Currency = "XOF",
            Status = TransactionStatus.Success,
            TransactionType = TransactionType.BankDebit
        };
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "ABC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var req = new TransactionRequest("ABC", Guid.NewGuid(), 1000, "XOF", null);
        var result = await handler.Handle(new BankDebitCommand(partnerId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransactionId.Should().Be(existing.TransactionId);
    }

    [Fact]
    public async Task Fails_when_subscription_invalid()
    {
        var handler = BuildHandler(out var txs, out var subs, out var partners, out _, out _);
        var partnerId = Guid.NewGuid();
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "REF1", It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>())).ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Active });
        subs.Setup(x => x.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync((Subscription?)null);

        var req = new TransactionRequest("REF1", Guid.NewGuid(), 1000, "XOF", null);
        var result = await handler.Handle(new BankDebitCommand(partnerId, req), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("SUBSCRIPTION_INVALID");
    }

    [Fact]
    public async Task Marks_transaction_as_failed_when_bank_returns_error()
    {
        var handler = BuildHandler(out var txs, out var subs, out var partners, out var bank, out _);
        var partnerId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        txs.Setup(x => x.GetByPartnerRefAsync(partnerId, "REF2", It.IsAny<CancellationToken>())).ReturnsAsync((Transaction?)null);
        partners.Setup(x => x.GetByIdAsync(partnerId, It.IsAny<CancellationToken>())).ReturnsAsync(new Partner { PartnerId = partnerId, Status = PartnerStatus.Active });
        subs.Setup(x => x.GetByIdAsync(subscriptionId, It.IsAny<CancellationToken>())).ReturnsAsync(new Subscription
        {
            SubscriptionId = subscriptionId,
            PartnerId = partnerId,
            CustomerId = Guid.NewGuid(),
            BankAccountNumber = "ACC123",
            PhoneNumber = "+22177",
            Status = SubscriptionStatus.Active
        });
        bank.Setup(x => x.DebitAsync(It.IsAny<Partner>(), It.IsAny<BankTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankTransactionResponse("EXT-1", "FAILED", "INSUFFICIENT_FUNDS"));

        var req = new TransactionRequest("REF2", subscriptionId, 1000, "XOF", null);
        var result = await handler.Handle(new BankDebitCommand(partnerId, req), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(TransactionStatus.Failed);
        result.Value.FailureReason.Should().Be("INSUFFICIENT_FUNDS");
    }
}
