-- =============================================================================
-- SeedDemoExtra.sql (apres SeedData.sql + TestData.sql)
-- Cree quelques utilisateurs additionnels (admin, finance, partner_demo).
-- Plus de seed direct de Transactions/Movements ; ils se creent via l'API.
-- =============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @PasswordHash NVARCHAR(200) = '$2a$12$0WJ30sGfLNi4h1GPz7Kjqukl/maXtwT0o56DW.Q1OdOkigBwR0o3q';

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'finance')
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'finance', 'finance@aggregator.local', @PasswordHash, 2, 1, 0, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'admin', 'admin@aggregator.local', @PasswordHash, 1, 1, 0, GETUTCDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'partner_demo')
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt, IsDeleted)
    VALUES (NEWID(), 'partner_demo', 'partner@bank-demo.local', @PasswordHash, 3, 1, 0, GETUTCDATE(), 0);

PRINT '--- SeedDemoExtra : ok ---';
SELECT 'Users' AS Entite, COUNT(*) AS N FROM Users;
