namespace AggregatorPlatform.Application.DTOs;

public record AdminDashboardSummaryDto(
    int TotalPartners,
    int ActivePartners,
    int TotalCustomers,
    int TotalSubscriptions,
    int TodayTransactions,
    decimal TodayVolume,
    int PendingTransactions,
    int FailedTransactions24h);

public record PartnerDashboardSummaryDto(
    Guid PartnerId,
    string PartnerCode,
    decimal AccountBalance,
    int TodayTransactions,
    decimal TodayVolume,
    int ActiveSubscriptions);
