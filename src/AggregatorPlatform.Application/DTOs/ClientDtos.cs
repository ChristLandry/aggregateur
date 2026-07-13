namespace AggregatorPlatform.Application.DTOs;

/// <summary>Vue synthetique d'un Client racine (n'inclut pas ses Customers).</summary>
public record ClientDto(
    Guid ClientId,
    string BankAccountRoot,
    string FullName,
    DateOnly? DateOfBirth,
    string? NationalId,
    string? PhoneNumber,
    string? Email,
    int CustomersCount,
    DateTime CreatedAt);

/// <summary>Detail d'un Client avec la liste de ses Customers rattaches.</summary>
public record ClientDetailDto(
    Guid ClientId,
    string BankAccountRoot,
    string FullName,
    DateOnly? DateOfBirth,
    string? NationalId,
    string? PhoneNumber,
    string? Email,
    DateTime CreatedAt,
    IReadOnlyList<ClientCustomerDto> Customers);

public record ClientCustomerDto(
    Guid CustomerId,
    string FullName,
    string? NationalId,
    string? Email,
    int Status,
    int KycStatus,
    DateTime CreatedAt);
