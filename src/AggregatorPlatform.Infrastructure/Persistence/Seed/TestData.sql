-- =============================================================================
-- TestData.sql
-- A executer APRES SeedData.sql.
-- Cree donnees de demo coherentes avec le NOUVEAU modele comptable :
--   - 2 Partners (BANK_DEMO, WALLET_DEMO) avec PartnerAccount + PartnerBankAccount
--   - 2 Customers
--   - 2 Subscriptions
--   - 2 AccountingSchemas (BankDebit, WalletDebit) avec lignes Code/Exploitant/IsFee
--     et formules referencant les lignes precedentes (L1, L2, ...)
-- Pas de FeeConfigurations ni JournalEntries (entites supprimees).
-- =============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- =================== PARTNERS ===================
IF NOT EXISTS (SELECT 1 FROM Partners WHERE PartnerId = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO Partners (PartnerId, PartnerCode, Name, BaseUrl, ApiKey, AccountCode, Status, Currency,
                          WebhookUrl, IpWhitelist, CreatedAt, IsDeleted)
    VALUES (
        '11111111-1111-1111-1111-111111111111',
        'BANK_DEMO', N'Banque Demo SA', 'http://localhost:5080',
        '8e3a4ee5e2e5e1e6d7f8a9b0c1d2e3f405a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0',
        'P-BANK_DEMO', 1, 'XOF',
        'https://webhook.bank-demo.local/aggregator',
        NULL, GETUTCDATE(), 0);

    INSERT INTO PartnerAccounts (AccountId, PartnerId, PartnerBankAccount, Balance, Currency, CreatedAt, IsDeleted)
    VALUES (NEWID(), '11111111-1111-1111-1111-111111111111', '0101010101010', 5000000, 'XOF', GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM Partners WHERE PartnerId = '22222222-2222-2222-2222-222222222222')
BEGIN
    INSERT INTO Partners (PartnerId, PartnerCode, Name, BaseUrl, ApiKey, AccountCode, Status, Currency,
                          WebhookUrl, IpWhitelist, CreatedAt, IsDeleted)
    VALUES (
        '22222222-2222-2222-2222-222222222222',
        'WALLET_DEMO', N'Wallet Demo (Orange Money)', 'http://localhost:5080',
        '9f4b5ff6f3f6f2f7e8f9b0c1d2e3f4a5b607c8d9e0f1a2b3c4d5e6f7a8b9c0d1',
        'P-WALLET_DEMO', 1, 'XOF',
        'https://webhook.wallet-demo.local/aggregator',
        NULL, GETUTCDATE(), 0);

    INSERT INTO PartnerAccounts (AccountId, PartnerId, PartnerBankAccount, Balance, Currency, CreatedAt, IsDeleted)
    VALUES (NEWID(), '22222222-2222-2222-2222-222222222222', '0202020202020', 3000000, 'XOF', GETUTCDATE(), 0);
END

-- =================== CUSTOMERS ===================
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
BEGIN
    INSERT INTO Customers (CustomerId, ExternalCustomerId, FullName, DateOfBirth, NationalId, Email,
                           Status, KycStatus, CreatedAt, IsDeleted)
    VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'EXT-CUST-001',
            N'Aissatou Diallo', '1990-04-12', 'SN-1234567890', 'aissatou.diallo@example.com',
            1, 2, GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
BEGIN
    INSERT INTO Customers (CustomerId, ExternalCustomerId, FullName, DateOfBirth, NationalId, Email,
                           Status, KycStatus, CreatedAt, IsDeleted)
    VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'EXT-CUST-002',
            N'Mamadou Sow', '1985-09-23', 'SN-9876543210', 'mamadou.sow@example.com',
            1, 2, GETUTCDATE(), 0);
END

-- =================== SUBSCRIPTIONS ===================
IF NOT EXISTS (SELECT 1 FROM Subscriptions WHERE SubscriptionId = '11111111-aaaa-aaaa-aaaa-111111111111')
BEGIN
    INSERT INTO Subscriptions (SubscriptionId, CustomerId, PartnerId, BankAccountNumber,
                               PhoneNumber, PhoneOperator, Status, SubscribedAt, CreatedAt, IsDeleted)
    VALUES ('11111111-aaaa-aaaa-aaaa-111111111111',
            'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            '11111111-1111-1111-1111-111111111111',
            'SN012-0001-2345-6789',
            '+221771112233', 'Orange',
            1, GETUTCDATE(), GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM Subscriptions WHERE SubscriptionId = '22222222-bbbb-bbbb-bbbb-222222222222')
BEGIN
    INSERT INTO Subscriptions (SubscriptionId, CustomerId, PartnerId, BankAccountNumber,
                               PhoneNumber, PhoneOperator, Status, SubscribedAt, CreatedAt, IsDeleted)
    VALUES ('22222222-bbbb-bbbb-bbbb-222222222222',
            'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            '22222222-2222-2222-2222-222222222222',
            'SN012-9999-8888-7777',
            '+221774445566', 'Wave',
            1, GETUTCDATE(), GETUTCDATE(), 0);
END

-- =================== ACCOUNTING SCHEMAS (NOUVEAU MODELE) ===================
-- Schema 1 : BankDebit standard
--   L1 : Debit compte client     = AMOUNT
--   L2 : Credit commission       = AMOUNT * 0.05      (IsFee = 1)
--   L3 : Credit vente nette      = L1 - L2            (utilise les lignes precedentes)
DECLARE @SchemaBankDebit UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
IF NOT EXISTS (SELECT 1 FROM AccountingSchemas WHERE SchemaId = @SchemaBankDebit)
BEGIN
    INSERT INTO AccountingSchemas (SchemaId, Name, PartnerId, TransactionType, TransactionSide, Channel,
                                    IsActive, Priority, Description, CreatedAt, IsDeleted)
    VALUES (@SchemaBankDebit, N'BankDebit standard XOF', NULL, 0, 0, 0, 1, 100,
            N'Debit bancaire avec commission 5% et ligne nette = L1 - L2', GETUTCDATE(), 0);

    INSERT INTO AccountingSchemaLines (LineId, SchemaId, LineOrder, AccountCode, AccountType, Side,
                                        AmountFormula, Label, Code, Exploitant, IsFee,
                                        IsConditional, Condition, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @SchemaBankDebit, 1, '411',    0, 0, 'AMOUNT',        N'Debit compte client',     'CLI',   'AU', 0, 0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaBankDebit, 2, '70-FEE', 0, 1, 'AMOUNT * 0.05', N'Commission agregateur',   'FEE',   'AU', 1, 0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaBankDebit, 3, '707',    0, 1, 'L1 - L2',       N'Vente nette',             'INTEG', 'AU', 0, 0, NULL, GETUTCDATE(), 0);
END

-- Schema 2 : WalletDebit standard
DECLARE @SchemaWalletDebit UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
IF NOT EXISTS (SELECT 1 FROM AccountingSchemas WHERE SchemaId = @SchemaWalletDebit)
BEGIN
    INSERT INTO AccountingSchemas (SchemaId, Name, PartnerId, TransactionType, TransactionSide, Channel,
                                    IsActive, Priority, Description, CreatedAt, IsDeleted)
    VALUES (@SchemaWalletDebit, N'WalletDebit standard XOF', NULL, 2, 0, 1, 1, 100,
            N'Debit wallet avec commission 2% et ligne nette = L1 - L2', GETUTCDATE(), 0);

    INSERT INTO AccountingSchemaLines (LineId, SchemaId, LineOrder, AccountCode, AccountType, Side,
                                        AmountFormula, Label, Code, Exploitant, IsFee,
                                        IsConditional, Condition, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @SchemaWalletDebit, 1, '411',    0, 0, 'AMOUNT',        N'Debit compte client',     'CLI',   'AU', 0, 0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaWalletDebit, 2, '70-FEE', 0, 1, 'AMOUNT * 0.02', N'Commission wallet',       'FEE',   'AU', 1, 0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaWalletDebit, 3, '707',    0, 1, 'L1 - L2',       N'Vente nette wallet',      'INTEG', 'AU', 0, 0, NULL, GETUTCDATE(), 0);
END

PRINT '=== TestData.sql applique avec succes ===';
PRINT 'Partners        : BANK_DEMO + WALLET_DEMO (PartnerAccount + PartnerBankAccount)';
PRINT 'Customers       : Aissatou + Mamadou';
PRINT 'Subscriptions   : 2';
PRINT 'AccountingSchemas : 2 (BankDebit + WalletDebit, formules L1/L2 + ligne IsFee)';
