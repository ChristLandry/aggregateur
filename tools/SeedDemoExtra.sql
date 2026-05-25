-- =============================================================================
-- SeedDemoExtra.sql
-- A executer APRES SeedData.sql + TestData.sql
-- Cree un jeu de donnees riche couvrant : transactions (success / pending / failed),
-- journal entries + lignes (comptabilisation), mouvements de comptes partenaires,
-- webhook logs, audit logs et refresh tokens.
-- Idempotent (toutes les inserts sont conditionnees a un IF NOT EXISTS).
-- =============================================================================

SET NOCOUNT ON;
PRINT '--- SeedDemoExtra : demarrage ---';

------------------------------------------------------------------------------
-- IDs reutilisables
------------------------------------------------------------------------------
DECLARE @PartnerBank    UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @PartnerWallet  UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Customer1      UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @Customer2      UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @Subscription1  UNIQUEIDENTIFIER = '11111111-aaaa-aaaa-aaaa-111111111111';
DECLARE @Subscription2  UNIQUEIDENTIFIER = '22222222-bbbb-bbbb-bbbb-222222222222';
DECLARE @SchemaBankDb   UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @SchemaWalletDb UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';

------------------------------------------------------------------------------
-- ADMIN secondaire (Finance) + utilisateur Partner
------------------------------------------------------------------------------
-- Mot de passe = 'ChangeMe123!' (BCrypt work-factor 12, meme hash que superadmin)
DECLARE @PasswordHash NVARCHAR(200) = '$2a$12$0WJ30sGfLNi4h1GPz7Kjqukl/maXtwT0o56DW.Q1OdOkigBwR0o3q';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'finance')
BEGIN
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'finance', 'finance@aggregator.local', @PasswordHash, 2, 1, 0, GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'admin', 'admin@aggregator.local', @PasswordHash, 1, 1, 0, GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'partner_demo')
BEGIN
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'partner_demo', 'partner@bank-demo.local', @PasswordHash, 3, 1, 0, GETUTCDATE(), 0);
END;

------------------------------------------------------------------------------
-- TRANSACTIONS demo (5)
--   T1 : BankDebit  / Success  / Applied
--   T2 : WalletDebit / Success / Applied
--   T3 : BankCredit / Success  / Pending (en attente de compta)
--   T4 : WalletDebit / Failed  / Pending (echec partenaire)
--   T5 : BankDebit  / Pending  / Pending (en cours)
------------------------------------------------------------------------------
DECLARE @T1 UNIQUEIDENTIFIER = '10000001-0000-0000-0000-000000000001';
DECLARE @T2 UNIQUEIDENTIFIER = '10000001-0000-0000-0000-000000000002';
DECLARE @T3 UNIQUEIDENTIFIER = '10000001-0000-0000-0000-000000000003';
DECLARE @T4 UNIQUEIDENTIFIER = '10000001-0000-0000-0000-000000000004';
DECLARE @T5 UNIQUEIDENTIFIER = '10000001-0000-0000-0000-000000000005';

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @T1)
BEGIN
    INSERT INTO Transactions (TransactionId, PartnerTransactionRef, PartnerId, SubscriptionId, CustomerId,
        TransactionType, Amount, FeeAmount, NetAmount, Currency, Status, FailureReason,
        AccountingStatus, SchemaId, InitiatedAt, CompletedAt, ExternalRef, CreatedAt, IsDeleted)
    VALUES (@T1, 'BANK-REF-0001', @PartnerBank, @Subscription1, @Customer1,
        0, 50000, 750, 49250, 'XOF', 1, NULL,
        1, @SchemaBankDb, DATEADD(DAY,-3, GETUTCDATE()), DATEADD(DAY,-3, GETUTCDATE()), 'EXT-A1', GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @T2)
BEGIN
    INSERT INTO Transactions (TransactionId, PartnerTransactionRef, PartnerId, SubscriptionId, CustomerId,
        TransactionType, Amount, FeeAmount, NetAmount, Currency, Status, FailureReason,
        AccountingStatus, SchemaId, InitiatedAt, CompletedAt, ExternalRef, CreatedAt, IsDeleted)
    VALUES (@T2, 'WAL-REF-0002', @PartnerWallet, @Subscription2, @Customer2,
        2, 25000, 500, 24500, 'XOF', 1, NULL,
        1, @SchemaWalletDb, DATEADD(DAY,-2, GETUTCDATE()), DATEADD(DAY,-2, GETUTCDATE()), 'EXT-A2', GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @T3)
BEGIN
    INSERT INTO Transactions (TransactionId, PartnerTransactionRef, PartnerId, SubscriptionId, CustomerId,
        TransactionType, Amount, FeeAmount, NetAmount, Currency, Status, FailureReason,
        AccountingStatus, SchemaId, InitiatedAt, CompletedAt, ExternalRef, CreatedAt, IsDeleted)
    VALUES (@T3, 'BANK-REF-0003', @PartnerBank, @Subscription1, @Customer1,
        1, 100000, 1000, 99000, 'XOF', 1, NULL,
        0, NULL, DATEADD(DAY,-1, GETUTCDATE()), DATEADD(DAY,-1, GETUTCDATE()), 'EXT-A3', GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @T4)
BEGIN
    INSERT INTO Transactions (TransactionId, PartnerTransactionRef, PartnerId, SubscriptionId, CustomerId,
        TransactionType, Amount, FeeAmount, NetAmount, Currency, Status, FailureReason,
        AccountingStatus, SchemaId, InitiatedAt, CompletedAt, ExternalRef, CreatedAt, IsDeleted)
    VALUES (@T4, 'WAL-REF-0004', @PartnerWallet, @Subscription2, @Customer2,
        2, 10000, 200, 9800, 'XOF', 2, 'Insufficient funds',
        0, NULL, DATEADD(HOUR,-12, GETUTCDATE()), NULL, NULL, GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM Transactions WHERE TransactionId = @T5)
BEGIN
    INSERT INTO Transactions (TransactionId, PartnerTransactionRef, PartnerId, SubscriptionId, CustomerId,
        TransactionType, Amount, FeeAmount, NetAmount, Currency, Status, FailureReason,
        AccountingStatus, SchemaId, InitiatedAt, CompletedAt, ExternalRef, CreatedAt, IsDeleted)
    VALUES (@T5, 'BANK-REF-0005', @PartnerBank, @Subscription1, @Customer1,
        0, 75000, 1125, 73875, 'XOF', 0, NULL,
        0, NULL, DATEADD(MINUTE,-15, GETUTCDATE()), NULL, NULL, GETUTCDATE(), 0);
END;

------------------------------------------------------------------------------
-- JOURNAL ENTRIES (uniquement pour T1 et T2 qui sont AccountingStatus = Applied)
------------------------------------------------------------------------------
DECLARE @JE1 UNIQUEIDENTIFIER = '20000001-0000-0000-0000-000000000001';
DECLARE @JE2 UNIQUEIDENTIFIER = '20000001-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM JournalEntries WHERE EntryId = @JE1)
BEGIN
    INSERT INTO JournalEntries (EntryId, TransactionId, SchemaId, EntryDate, TotalDebit, TotalCredit, IsBalanced, CreatedAt, IsDeleted)
    VALUES (@JE1, @T1, @SchemaBankDb, DATEADD(DAY,-3, GETUTCDATE()), 50000, 50000, 1, GETUTCDATE(), 0);

    INSERT INTO JournalLines (LineId, EntryId, AccountCode, Side, Amount, Label, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @JE1, '411',    0, 50000, N'Compte client',         GETUTCDATE(), 0),
        (NEWID(), @JE1, '707',    1, 49250, N'Vente nette',           GETUTCDATE(), 0),
        (NEWID(), @JE1, '70-FEE', 1,   750, N'Commission agreg.',     GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM JournalEntries WHERE EntryId = @JE2)
BEGIN
    INSERT INTO JournalEntries (EntryId, TransactionId, SchemaId, EntryDate, TotalDebit, TotalCredit, IsBalanced, CreatedAt, IsDeleted)
    VALUES (@JE2, @T2, @SchemaWalletDb, DATEADD(DAY,-2, GETUTCDATE()), 25000, 25000, 1, GETUTCDATE(), 0);

    INSERT INTO JournalLines (LineId, EntryId, AccountCode, Side, Amount, Label, CreatedAt, IsDeleted)
    VALUES
        (NEWID(), @JE2, '411',    0, 25000, N'Compte client',         GETUTCDATE(), 0),
        (NEWID(), @JE2, '707',    1, 24500, N'Vente nette wallet',    GETUTCDATE(), 0),
        (NEWID(), @JE2, '70-FEE', 1,   500, N'Commission wallet',     GETUTCDATE(), 0);
END;

------------------------------------------------------------------------------
-- PARTNER ACCOUNT MOVEMENTS
------------------------------------------------------------------------------
DECLARE @BankAccountId   UNIQUEIDENTIFIER = (SELECT TOP 1 AccountId FROM PartnerAccounts WHERE PartnerId = @PartnerBank);
DECLARE @WalletAccountId UNIQUEIDENTIFIER = (SELECT TOP 1 AccountId FROM PartnerAccounts WHERE PartnerId = @PartnerWallet);

IF @BankAccountId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PartnerAccountMovements WHERE TransactionId = @T1)
BEGIN
    INSERT INTO PartnerAccountMovements (MovementId, PartnerId, TransactionId, MovementType, Amount,
        BalanceBefore, BalanceAfter, MovementDate, Description, PartnerAccountAccountId, CreatedAt, IsDeleted)
    VALUES (NEWID(), @PartnerBank, @T1, 1, 50000,
        5000000, 4950000, DATEADD(DAY,-3, GETUTCDATE()), N'Debit BANK_DEMO #0001', @BankAccountId, GETUTCDATE(), 0);
END;

IF @WalletAccountId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PartnerAccountMovements WHERE TransactionId = @T2)
BEGIN
    INSERT INTO PartnerAccountMovements (MovementId, PartnerId, TransactionId, MovementType, Amount,
        BalanceBefore, BalanceAfter, MovementDate, Description, PartnerAccountAccountId, CreatedAt, IsDeleted)
    VALUES (NEWID(), @PartnerWallet, @T2, 1, 25000,
        3000000, 2975000, DATEADD(DAY,-2, GETUTCDATE()), N'Debit WALLET_DEMO #0002', @WalletAccountId, GETUTCDATE(), 0);
END;

IF @BankAccountId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM PartnerAccountMovements WHERE TransactionId = @T3)
BEGIN
    INSERT INTO PartnerAccountMovements (MovementId, PartnerId, TransactionId, MovementType, Amount,
        BalanceBefore, BalanceAfter, MovementDate, Description, PartnerAccountAccountId, CreatedAt, IsDeleted)
    VALUES (NEWID(), @PartnerBank, @T3, 0, 100000,
        4950000, 5050000, DATEADD(DAY,-1, GETUTCDATE()), N'Credit BANK_DEMO #0003', @BankAccountId, GETUTCDATE(), 0);
END;

-- Re-aligner les balances PartnerAccounts apres mouvements simules
UPDATE PartnerAccounts SET Balance = 5050000 WHERE PartnerId = @PartnerBank;
UPDATE PartnerAccounts SET Balance = 2975000 WHERE PartnerId = @PartnerWallet;

------------------------------------------------------------------------------
-- WEBHOOK LOGS
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM WebhookLogs WHERE TransactionId = @T1)
BEGIN
    INSERT INTO WebhookLogs (LogId, PartnerId, TransactionId, EventType, Payload, TargetUrl,
        AttemptCount, LastAttemptAt, NextAttemptAt, Status, ResponseCode, ResponseBody, CreatedAt, IsDeleted)
    VALUES (NEWID(), @PartnerBank, @T1, 'transaction.completed',
        '{"transactionId":"10000001-0000-0000-0000-000000000001","status":"Success"}',
        'https://webhook.bank-demo.local/aggregator',
        1, DATEADD(DAY,-3, GETUTCDATE()), NULL, 1, 200, 'OK', GETUTCDATE(), 0);
END;

IF NOT EXISTS (SELECT 1 FROM WebhookLogs WHERE TransactionId = @T4)
BEGIN
    INSERT INTO WebhookLogs (LogId, PartnerId, TransactionId, EventType, Payload, TargetUrl,
        AttemptCount, LastAttemptAt, NextAttemptAt, Status, ResponseCode, ResponseBody, CreatedAt, IsDeleted)
    VALUES (NEWID(), @PartnerWallet, @T4, 'transaction.failed',
        '{"transactionId":"10000001-0000-0000-0000-000000000004","status":"Failed","reason":"Insufficient funds"}',
        'https://webhook.wallet-demo.local/aggregator',
        3, DATEADD(HOUR,-1, GETUTCDATE()), DATEADD(HOUR, 1, GETUTCDATE()), 2, 503, 'Upstream timeout', GETUTCDATE(), 0);
END;

------------------------------------------------------------------------------
-- AUDIT LOGS
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM AuditLogs WHERE EntityId = CONVERT(NVARCHAR(100), @T1) AND Action = 'CREATE')
BEGIN
    INSERT INTO AuditLogs (LogId, EntityType, EntityId, Action, OldValues, NewValues,
        PerformedBy, PerformedAt, IpAddress, UserAgent)
    VALUES
        (NEWID(), 'Transaction', CONVERT(NVARCHAR(100), @T1), 'CREATE', NULL,
         '{"Amount":50000,"Status":"Pending"}', 'superadmin', DATEADD(DAY,-3, GETUTCDATE()),
         '127.0.0.1', 'Aggregator/1.0'),
        (NEWID(), 'Transaction', CONVERT(NVARCHAR(100), @T1), 'UPDATE',
         '{"Status":"Pending"}', '{"Status":"Success"}', 'system', DATEADD(DAY,-3, GETUTCDATE()),
         '127.0.0.1', 'Aggregator/1.0'),
        (NEWID(), 'Partner',     CONVERT(NVARCHAR(100), @PartnerBank), 'CREATE', NULL,
         '{"PartnerCode":"BANK_DEMO"}', 'superadmin', DATEADD(DAY,-7, GETUTCDATE()),
         '127.0.0.1', 'Aggregator/1.0');
END;

------------------------------------------------------------------------------
-- Sortie
------------------------------------------------------------------------------
DECLARE @TxCount INT = (SELECT COUNT(*) FROM Transactions);
DECLARE @JeCount INT = (SELECT COUNT(*) FROM JournalEntries);
DECLARE @MvCount INT = (SELECT COUNT(*) FROM PartnerAccountMovements);
DECLARE @WhCount INT = (SELECT COUNT(*) FROM WebhookLogs);
DECLARE @AlCount INT = (SELECT COUNT(*) FROM AuditLogs);
DECLARE @UsCount INT = (SELECT COUNT(*) FROM Users);

PRINT '--- SeedDemoExtra : termine ---';
PRINT CONCAT('Users                : ', @UsCount);
PRINT CONCAT('Transactions         : ', @TxCount);
PRINT CONCAT('JournalEntries       : ', @JeCount);
PRINT CONCAT('PartnerAccountMoves  : ', @MvCount);
PRINT CONCAT('WebhookLogs          : ', @WhCount);
PRINT CONCAT('AuditLogs            : ', @AlCount);
