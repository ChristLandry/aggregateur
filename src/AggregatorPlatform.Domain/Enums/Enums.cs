namespace AggregatorPlatform.Domain.Enums;

public enum PartnerStatus
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

/// <summary>
/// Canaux d'envoi des alertes partenaire (flags combinables).
/// </summary>
[Flags]
public enum AlertChannels
{
    None = 0,
    Email = 1,
    Sms = 2,
    EmailAndSms = Email | Sms,
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

/// <summary>
/// Liste des codes partenaire autorises pour la creation.
/// Le PartnerCode passe a POST /api/v1/partners doit obligatoirement correspondre
/// (sensibilite a la casse) au nom d'une de ces valeurs.
/// Pour ajouter un nouveau partenaire eligible : ajouter la valeur ici puis
/// reconstruire. Aucune migration n'est requise (stocke comme string en BD).
/// </summary>
public enum AllowedPartnerCode
{
    BANK_DEMO    = 0,
    WALLET_DEMO  = 1,
    ORANGE_MONEY = 2,
    WAVE         = 3,
    MTN_MOMO     = 4,
    MOOV_MONEY   = 5,
    FREE_MONEY   = 6,
    WIZALL       = 7,
    E_MONEY      = 8,

    /// <summary>
    /// Partenaire technique reserve a l'application web.
    /// Cree automatiquement au demarrage avec Partner.IsWebPartner = true.
    /// Restrictions : exclu de la liste publique GET /partners et
    /// interdit sur les routes /api/v1/financial/{bank|wallet}/*.
    /// </summary>
    WEB          = 9,
}

/// <summary>
/// Identifiants des endpoints financiers configurables par partenaire.
/// Mapping vers TransactionType :
///   BankDebit    -> TransactionType.BankDebit    (0)
///   BankCredit   -> TransactionType.BankCredit   (1)
///   WalletDebit  -> TransactionType.WalletDebit  (2)
///   WalletCredit -> TransactionType.WalletCredit (3)
/// WalletCancel n'est pas configurable : il est derive de la transaction d'origine.
/// </summary>
public enum FinancialEndpointKey
{
    BankDebit    = 0,
    BankCredit   = 1,
    WalletDebit  = 2,
    WalletCredit = 3,
}
