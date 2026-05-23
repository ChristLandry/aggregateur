namespace AggregatorPlatform.Domain.Enums;

public enum PartnerStatus
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

public enum CustomerStatus
{
    Inactive = 0,
    Active = 1,
    Blocked = 2
}

public enum KycStatus
{
    NotVerified = 0,
    InProgress = 1,
    Verified = 2,
    Rejected = 3
}

public enum SubscriptionStatus
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

public enum TransactionType
{
    BankDebit = 0,
    BankCredit = 1,
    WalletDebit = 2,
    WalletCredit = 3,
    WalletCancel = 4
}

public enum TransactionSide
{
    Debit = 0,
    Credit = 1
}

public enum Channel
{
    Bank = 0,
    Wallet = 1
}

public enum TransactionStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Cancelled = 3,
    Reversed = 4
}

public enum AccountingStatus
{
    Pending = 0,
    Applied = 1,
    Error = 2
}

public enum AccountType
{
    Fixed = 0,
    Dynamic = 1
}

public enum LedgerSide
{
    Debit = 0,
    Credit = 1
}

public enum MovementType
{
    Credit = 0,
    Debit = 1
}

public enum WebhookStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2
}

public enum FeeType
{
    Fixed = 0,
    Percentage = 1,
    Mixed = 2
}

public enum UserRole
{
    SuperAdmin = 0,
    Admin = 1,
    Finance = 2,
    Partner = 3,
    ReadOnly = 4
}
