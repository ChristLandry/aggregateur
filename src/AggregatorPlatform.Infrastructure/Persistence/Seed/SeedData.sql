-- =============================================================================
-- Seed script: SUPER_ADMIN user + default system parameters
-- Password for super_admin (BCrypt hash of "ChangeMe123!"): regenerate in production!
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'superadmin')
BEGIN
    INSERT INTO Users (UserId, Username, Email, PasswordHash, Role, IsActive, TwoFactorEnabled, CreatedAt)
    VALUES (NEWID(), 'superadmin', 'superadmin@aggregator.local',
            '$2a$12$abcdefghijklmnopqrstuv1234567890abcdefghijklmnopqrstuvwxyz1234',
            0, 1, 0, GETUTCDATE());
END

MERGE SystemParameters AS target
USING (VALUES
    ('DEFAULT_TIMEOUT_MS', '30000', 'Default HTTP timeout to partner APIs (ms)'),
    ('MAX_RETRY_COUNT', '3', 'Maximum retry attempts'),
    ('PENDING_RECONCILIATION_MINUTES', '30', 'Pending transactions older than this are reconciled'),
    ('RATE_LIMIT_DEFAULT', '100', 'Default requests per minute per partner'),
    ('CACHE_PARTNER_TTL_SECONDS', '300', 'Partner cache TTL in seconds'),
    ('LOG_RETENTION_DAYS', '90', 'Audit and webhook log retention in days'),
    ('CACHE_DASHBOARD_TTL_SECONDS', '30', 'Dashboard cache TTL'),
    ('CACHE_KYC_TTL_SECONDS', '120', 'KYC cache TTL')
) AS source ([Key], Value, Description)
ON target.[Key] = source.[Key]
WHEN NOT MATCHED THEN
    INSERT ([Key], Value, Description, UpdatedAt) VALUES (source.[Key], source.Value, source.Description, GETUTCDATE());
