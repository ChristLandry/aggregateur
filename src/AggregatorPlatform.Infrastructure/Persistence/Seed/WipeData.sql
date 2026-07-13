-- =============================================================================
-- WipeData.sql
-- Reset des donnees applicatives, en conservant :
--   * le partenaire WEB (IsWebPartner = 1) auto-seede
--   * tous les utilisateurs / RefreshTokens (auth JWT)
--   * les parametres systeme (SystemParameters)
--
-- Ordre : enfants -> parents (respecte les FK).
-- =============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Journaux / audit / mouvements comptables (dependent de Transactions & Partners)
DELETE FROM Movements;
DELETE FROM AuditLogs;
DELETE FROM WebhookLogs;

-- Transactions (avant Subscriptions)
DELETE FROM Transactions;

-- Chaine Subscription -> Customer -> Client
DELETE FROM Subscriptions;
DELETE FROM Customers;
DELETE FROM Clients;

-- Schemas comptables (avant Partners)
DELETE FROM AccountingSchemaLines;
DELETE FROM AccountingSchemas;

-- Endpoints partenaires + comptes/mouvements partenaires
DELETE FROM PartnerEndpoints;
DELETE FROM PartnerAccountMovements;
DELETE FROM PartnerAccounts;

-- Partners : on garde uniquement le WEB (auto-seede au demarrage)
DELETE FROM Partners WHERE IsWebPartner = 0;

-- SANITY : on ne touche PAS a Users, RefreshTokens, SystemParameters.

PRINT '=== WipeData.sql applique : partner WEB + users conserves ===';
