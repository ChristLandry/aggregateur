-- =============================================================================
-- wipe-data.sql
-- Supprime TOUTES les donnees applicatives en respectant l'ordre des FK.
-- Conserve le schema (tables, contraintes) et __EFMigrationsHistory.
-- =============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
PRINT '--- Wipe AggregatorDB : debut ---';

DELETE FROM AuditLogs;
DELETE FROM WebhookLogs;
DELETE FROM JournalLines;
DELETE FROM JournalEntries;
DELETE FROM PartnerAccountMovements;
DELETE FROM Transactions;
DELETE FROM AccountingSchemaLines;
DELETE FROM AccountingSchemas;
DELETE FROM FeeConfigurations;
DELETE FROM Subscriptions;
DELETE FROM Customers;
DELETE FROM PartnerAccounts;
DELETE FROM Partners;
DELETE FROM RefreshTokens;
DELETE FROM Users;
DELETE FROM SystemParameters;

PRINT '--- Wipe termine ---';
SELECT 'AuditLogs'              AS Entite, COUNT(*) AS N FROM AuditLogs UNION ALL
SELECT 'WebhookLogs',              COUNT(*) FROM WebhookLogs UNION ALL
SELECT 'JournalLines',             COUNT(*) FROM JournalLines UNION ALL
SELECT 'JournalEntries',           COUNT(*) FROM JournalEntries UNION ALL
SELECT 'PartnerAccountMovements',  COUNT(*) FROM PartnerAccountMovements UNION ALL
SELECT 'Transactions',             COUNT(*) FROM Transactions UNION ALL
SELECT 'AccountingSchemaLines',    COUNT(*) FROM AccountingSchemaLines UNION ALL
SELECT 'AccountingSchemas',        COUNT(*) FROM AccountingSchemas UNION ALL
SELECT 'FeeConfigurations',        COUNT(*) FROM FeeConfigurations UNION ALL
SELECT 'Subscriptions',            COUNT(*) FROM Subscriptions UNION ALL
SELECT 'Customers',                COUNT(*) FROM Customers UNION ALL
SELECT 'PartnerAccounts',          COUNT(*) FROM PartnerAccounts UNION ALL
SELECT 'Partners',                 COUNT(*) FROM Partners UNION ALL
SELECT 'RefreshTokens',            COUNT(*) FROM RefreshTokens UNION ALL
SELECT 'Users',                    COUNT(*) FROM Users UNION ALL
SELECT 'SystemParameters',         COUNT(*) FROM SystemParameters;
