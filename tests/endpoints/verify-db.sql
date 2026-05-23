-- =============================================================================
-- Requetes de verification post-tests
-- A executer apres test-all-endpoints.ps1 pour verifier que les donnees
-- sont bien presentes en base.
-- =============================================================================

USE AggregatorDB;
GO

PRINT '=== PARTENAIRES ===';
SELECT PartnerId, PartnerCode, Name, Status, Currency, CreatedAt
FROM Partners
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '=== COMPTES MIROIRS ===';
SELECT pa.AccountId, p.PartnerCode, pa.Balance, pa.Currency, pa.LastMovementAt
FROM PartnerAccounts pa
JOIN Partners p ON p.PartnerId = pa.PartnerId
ORDER BY pa.LastMovementAt DESC;

PRINT '';
PRINT '=== CLIENTS ===';
SELECT CustomerId, ExternalCustomerId, FullName, KycStatus, Status, CreatedAt
FROM Customers
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '=== SOUSCRIPTIONS (joinees au client + partenaire) ===';
SELECT s.SubscriptionId,
       c.FullName            AS Client,
       p.PartnerCode         AS Partenaire,
       s.PhoneOperator,
       s.Status,
       s.SubscribedAt
FROM Subscriptions s
JOIN Customers c ON c.CustomerId = s.CustomerId
JOIN Partners  p ON p.PartnerId  = s.PartnerId
ORDER BY s.SubscribedAt DESC;

PRINT '';
PRINT '=== TRANSACTIONS (20 dernieres) ===';
SELECT TOP 20
       t.TransactionId,
       t.PartnerTransactionRef,
       p.PartnerCode,
       t.TransactionType,
       t.Amount,
       t.FeeAmount,
       t.NetAmount,
       t.Status,
       t.AccountingStatus,
       t.InitiatedAt,
       t.CompletedAt
FROM Transactions t
JOIN Partners p ON p.PartnerId = t.PartnerId
ORDER BY t.InitiatedAt DESC;

PRINT '';
PRINT '=== MOUVEMENTS COMPTE MIROIR ===';
SELECT TOP 20
       pm.MovementId,
       p.PartnerCode,
       pm.MovementType,
       pm.Amount,
       pm.BalanceBefore,
       pm.BalanceAfter,
       pm.MovementDate,
       pm.Description
FROM PartnerAccountMovements pm
JOIN Partners p ON p.PartnerId = pm.PartnerId
ORDER BY pm.MovementDate DESC;

PRINT '';
PRINT '=== ECRITURES COMPTABLES ===';
SELECT TOP 20
       je.EntryId,
       je.TransactionId,
       sch.Name AS SchemaName,
       je.EntryDate,
       je.TotalDebit,
       je.TotalCredit,
       je.IsBalanced
FROM JournalEntries je
JOIN AccountingSchemas sch ON sch.SchemaId = je.SchemaId
ORDER BY je.EntryDate DESC;

PRINT '';
PRINT '=== LIGNES D''ECRITURE (5 dernieres ecritures) ===';
SELECT TOP 50
       jl.LineId,
       jl.EntryId,
       jl.AccountCode,
       jl.Side,
       jl.Amount,
       jl.Label,
       je.EntryDate
FROM JournalLines jl
JOIN JournalEntries je ON je.EntryId = jl.EntryId
ORDER BY je.EntryDate DESC, jl.AccountCode;

PRINT '';
PRINT '=== SCHEMAS COMPTABLES ===';
SELECT s.SchemaId,
       s.Name,
       s.TransactionType,
       s.TransactionSide,
       s.Channel,
       s.IsActive,
       s.Priority,
       (SELECT COUNT(*) FROM AccountingSchemaLines WHERE SchemaId = s.SchemaId) AS NbLines
FROM AccountingSchemas s
ORDER BY s.Priority;

PRINT '';
PRINT '=== CONFIGURATIONS DE FRAIS ===';
SELECT FeeId, PartnerId, TransactionType, FeeType, FixedAmount, Percentage, MaxFeeAmount, IsActive
FROM FeeConfigurations
ORDER BY TransactionType;

PRINT '';
PRINT '=== AUDIT LOG (10 dernieres entrees) ===';
SELECT TOP 10
       EntityType,
       Action,
       PerformedBy,
       PerformedAt,
       LEFT(NewValues, 100) AS NewValuesPreview
FROM AuditLogs
ORDER BY PerformedAt DESC;

PRINT '';
PRINT '=== WEBHOOKS EN ATTENTE ===';
SELECT TOP 20
       LogId, PartnerId, EventType, Status, AttemptCount, NextAttemptAt, LastAttemptAt
FROM WebhookLogs
ORDER BY CreatedAt DESC;

PRINT '';
PRINT '=== STATISTIQUES GLOBALES ===';
SELECT
    (SELECT COUNT(*) FROM Partners WHERE IsDeleted = 0)            AS NbPartenaires,
    (SELECT COUNT(*) FROM Customers WHERE IsDeleted = 0)           AS NbClients,
    (SELECT COUNT(*) FROM Subscriptions WHERE IsDeleted = 0)       AS NbSouscriptions,
    (SELECT COUNT(*) FROM Transactions)                            AS NbTransactions,
    (SELECT COUNT(*) FROM Transactions WHERE Status = 1)           AS NbTransactionsSuccess,
    (SELECT COUNT(*) FROM Transactions WHERE Status = 0)           AS NbTransactionsPending,
    (SELECT COUNT(*) FROM JournalEntries WHERE IsBalanced = 1)     AS NbEcrituresEquilibrees,
    (SELECT COUNT(*) FROM PartnerAccountMovements)                 AS NbMouvements;
