-- =============================================================================
-- TEST DATA — Aggregator Platform
-- A executer APRES SeedData.sql
-- Cree des donnees coherentes et previsibles pour tester tous les endpoints.
--
-- IDs fixes pour permettre les tests reproductibles :
--   Partner 1 (BANK_DEMO)     : 11111111-1111-1111-1111-111111111111
--   Partner 2 (WALLET_DEMO)   : 22222222-2222-2222-2222-222222222222
--   Customer 1 (Aissatou)     : aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
--   Customer 2 (Mamadou)      : bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb
--   Subscription 1            : 11111111-aaaa-aaaa-aaaa-111111111111
--   Subscription 2            : 22222222-bbbb-bbbb-bbbb-222222222222
--
-- Note : les colonnes chiffrees AES-256 (BankAccountNumber, PhoneNumber, NationalId)
-- sont inserees en clair ici a des fins de SEED uniquement.
-- L'application les chiffrera automatiquement sur les ecritures applicatives.
-- =============================================================================

SET NOCOUNT ON;

-- =================== PARTNERS ===================
IF NOT EXISTS (SELECT 1 FROM Partners WHERE PartnerId = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO Partners (PartnerId, PartnerCode, Name, BaseUrl, ApiKey, AccountCode, Status, Currency,
                          WebhookUrl, RateLimitPerMin, IpWhitelist, RequireHmac, CreatedAt, IsDeleted)
    VALUES (
        '11111111-1111-1111-1111-111111111111',
        'BANK_DEMO',
        'Banque Demo SA',
        'http://localhost:5080', -- pour dev local ; remplacer par https://api.bank-demo.local en prod
        -- SHA-256 de 'demo-api-key-bank-1234' (la cle en clair pour les tests = 'demo-api-key-bank-1234')
        '8e3a4ee5e2e5e1e6d7f8a9b0c1d2e3f405a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0',
        'P-BANK_DEMO',
        1, -- Active
        'XOF',
        'https://webhook.bank-demo.local/aggregator',
        500,
        NULL,
        0,
        GETUTCDATE(),
        0);

    INSERT INTO PartnerAccounts (AccountId, PartnerId, Balance, Currency, CreatedAt, IsDeleted)
    VALUES (NEWID(), '11111111-1111-1111-1111-111111111111', 5000000, 'XOF', GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM Partners WHERE PartnerId = '22222222-2222-2222-2222-222222222222')
BEGIN
    INSERT INTO Partners (PartnerId, PartnerCode, Name, BaseUrl, ApiKey, AccountCode, Status, Currency,
                          WebhookUrl, RateLimitPerMin, IpWhitelist, RequireHmac, CreatedAt, IsDeleted)
    VALUES (
        '22222222-2222-2222-2222-222222222222',
        'WALLET_DEMO',
        'Wallet Demo (Orange Money)',
        'http://localhost:5080', -- pour dev local ; remplacer par https://api.wallet-demo.local en prod
        -- SHA-256 de 'demo-api-key-wallet-5678'
        '9f4b5ff6f3f6f2f7e8f9b0c1d2e3f4a5b607c8d9e0f1a2b3c4d5e6f7a8b9c0d1',
        'P-WALLET_DEMO',
        1,
        'XOF',
        'https://webhook.wallet-demo.local/aggregator',
        1000,
        NULL,
        0,
        GETUTCDATE(),
        0);

    INSERT INTO PartnerAccounts (AccountId, PartnerId, Balance, Currency, CreatedAt, IsDeleted)
    VALUES (NEWID(), '22222222-2222-2222-2222-222222222222', 3000000, 'XOF', GETUTCDATE(), 0);
END

-- =================== CUSTOMERS ===================
IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
BEGIN
    INSERT INTO Customers (CustomerId, ExternalCustomerId, FullName, DateOfBirth, NationalId, Email,
                           Status, KycStatus, CreatedAt, IsDeleted)
    VALUES (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'EXT-CUST-001',
        N'Aissatou Diallo',
        '1990-04-12',
        'SN-1234567890',
        'aissatou.diallo@example.com',
        1, -- Active
        2, -- Verified
        GETUTCDATE(),
        0);
END

IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
BEGIN
    INSERT INTO Customers (CustomerId, ExternalCustomerId, FullName, DateOfBirth, NationalId, Email,
                           Status, KycStatus, CreatedAt, IsDeleted)
    VALUES (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        'EXT-CUST-002',
        N'Mamadou Sow',
        '1985-09-23',
        'SN-9876543210',
        'mamadou.sow@example.com',
        1,
        2,
        GETUTCDATE(),
        0);
END

-- =================== SUBSCRIPTIONS ===================
IF NOT EXISTS (SELECT 1 FROM Subscriptions WHERE SubscriptionId = '11111111-aaaa-aaaa-aaaa-111111111111')
BEGIN
    INSERT INTO Subscriptions (SubscriptionId, CustomerId, PartnerId, BankAccountNumber, BankCode,
                               PhoneNumber, PhoneOperator, Status, SubscribedAt, CreatedAt, IsDeleted)
    VALUES (
        '11111111-aaaa-aaaa-aaaa-111111111111',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        '11111111-1111-1111-1111-111111111111',
        'SN012-0001-2345-6789',
        'BANK_DEMO',
        '+221771112233',
        'Orange',
        1, -- Active
        GETUTCDATE(),
        GETUTCDATE(),
        0);
END

IF NOT EXISTS (SELECT 1 FROM Subscriptions WHERE SubscriptionId = '22222222-bbbb-bbbb-bbbb-222222222222')
BEGIN
    INSERT INTO Subscriptions (SubscriptionId, CustomerId, PartnerId, BankAccountNumber, BankCode,
                               PhoneNumber, PhoneOperator, Status, SubscribedAt, CreatedAt, IsDeleted)
    VALUES (
        '22222222-bbbb-bbbb-bbbb-222222222222',
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        '22222222-2222-2222-2222-222222222222',
        'SN012-9999-8888-7777',
        'WALLET_DEMO',
        '+221774445566',
        'Wave',
        1,
        GETUTCDATE(),
        GETUTCDATE(),
        0);
END

-- =================== FEE CONFIGURATIONS ===================
IF NOT EXISTS (SELECT 1 FROM FeeConfigurations WHERE TransactionType = 0 AND PartnerId IS NULL)
BEGIN
    INSERT INTO FeeConfigurations (FeeId, PartnerId, TransactionType, FeeType, FixedAmount, Percentage,
                                    MaxFeeAmount, IsActive, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), NULL, 0, 1, 0, 0.015, 5000, 1, GETUTCDATE(), 0),  -- BankDebit  : 1.5% cap 5000
        (NEWID(), NULL, 1, 1, 0, 0.010, 3000, 1, GETUTCDATE(), 0),  -- BankCredit : 1%  cap 3000
        (NEWID(), NULL, 2, 1, 0, 0.020, 5000, 1, GETUTCDATE(), 0),  -- WalletDebit : 2% cap 5000
        (NEWID(), NULL, 3, 1, 0, 0.015, 4000, 1, GETUTCDATE(), 0),  -- WalletCredit : 1.5% cap 4000
        (NEWID(), NULL, 4, 0, 0,     0,    0, 1, GETUTCDATE(), 0);  -- WalletCancel : 0
END

-- =================== ACCOUNTING SCHEMAS ===================
-- Schema : BankDebit (global)
DECLARE @SchemaBankDebit UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
IF NOT EXISTS (SELECT 1 FROM AccountingSchemas WHERE SchemaId = @SchemaBankDebit)
BEGIN
    INSERT INTO AccountingSchemas (SchemaId, Name, PartnerId, TransactionType, TransactionSide, Channel,
                                    IsActive, Priority, Description, CreatedAt, IsDeleted)
    VALUES (@SchemaBankDebit, 'BankDebit standard XOF', NULL, 0, 0, 0, 1, 100,
            N'Schema global pour les debits bancaires', GETUTCDATE(), 0);

    INSERT INTO AccountingSchemaLines (LineId, SchemaId, LineOrder, AccountCode, AccountType, Side,
                                        AmountFormula, Label, IsConditional, Condition, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @SchemaBankDebit, 1, '411',    0, 0, 'AMOUNT',     N'Compte client',      0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaBankDebit, 2, '707',    0, 1, 'AMOUNT_NET', N'Vente nette',         0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaBankDebit, 3, '70-FEE', 0, 1, 'FEE',        N'Commission agreg.',   1, 'FEE > 0', GETUTCDATE(), 0);
END

-- Schema : WalletDebit (global)
DECLARE @SchemaWalletDebit UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
IF NOT EXISTS (SELECT 1 FROM AccountingSchemas WHERE SchemaId = @SchemaWalletDebit)
BEGIN
    INSERT INTO AccountingSchemas (SchemaId, Name, PartnerId, TransactionType, TransactionSide, Channel,
                                    IsActive, Priority, Description, CreatedAt, IsDeleted)
    VALUES (@SchemaWalletDebit, 'WalletDebit standard XOF', NULL, 2, 0, 1, 1, 100,
            N'Schema global pour les debits wallet', GETUTCDATE(), 0);

    INSERT INTO AccountingSchemaLines (LineId, SchemaId, LineOrder, AccountCode, AccountType, Side,
                                        AmountFormula, Label, IsConditional, Condition, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @SchemaWalletDebit, 1, '411',    0, 0, 'AMOUNT',     N'Compte client',         0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaWalletDebit, 2, '707',    0, 1, 'AMOUNT_NET', N'Vente nette wallet',    0, NULL, GETUTCDATE(), 0),
        (NEWID(), @SchemaWalletDebit, 3, '70-FEE', 0, 1, 'FEE',        N'Commission wallet',     1, 'FEE > 0', GETUTCDATE(), 0);
END

PRINT '=== TestData.sql applique avec succes ===';
PRINT 'Partners        : 2 (BANK_DEMO, WALLET_DEMO)';
PRINT 'Customers       : 2 (Aissatou, Mamadou)';
PRINT 'Subscriptions   : 2';
PRINT 'FeeConfigs      : 5 (1 par TransactionType)';
PRINT 'Accounting      : 2 schemas (BankDebit + WalletDebit) avec 3 lignes chacun';
PRINT '';
PRINT 'IDs reutilisables :';
PRINT '  Partner BANK    : 11111111-1111-1111-1111-111111111111';
PRINT '  Partner WALLET  : 22222222-2222-2222-2222-222222222222';
PRINT '  Customer 1      : aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
PRINT '  Customer 2      : bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
PRINT '  Subscription 1  : 11111111-aaaa-aaaa-aaaa-111111111111';
PRINT '  Subscription 2  : 22222222-bbbb-bbbb-bbbb-222222222222';
