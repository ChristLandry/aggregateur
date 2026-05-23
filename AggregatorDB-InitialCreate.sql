IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [LogId] uniqueidentifier NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [PerformedBy] nvarchar(100) NOT NULL,
        [PerformedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([LogId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [CustomerId] uniqueidentifier NOT NULL,
        [ExternalCustomerId] nvarchar(100) NULL,
        [FullName] nvarchar(300) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [NationalId] nvarchar(500) NULL,
        [Email] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [KycStatus] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [Partners] (
        [PartnerId] uniqueidentifier NOT NULL,
        [PartnerCode] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [BaseUrl] nvarchar(500) NOT NULL,
        [ApiKey] nvarchar(500) NOT NULL,
        [AccountCode] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [WebhookUrl] nvarchar(500) NULL,
        [RateLimitPerMin] int NOT NULL,
        [IpWhitelist] nvarchar(1000) NULL,
        [RequireHmac] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Partners] PRIMARY KEY ([PartnerId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemParameters] (
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        [Description] nvarchar(500) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SystemParameters] PRIMARY KEY ([Key])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [AccountingSchemas] (
        [SchemaId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [PartnerId] uniqueidentifier NULL,
        [TransactionType] int NOT NULL,
        [TransactionSide] int NOT NULL,
        [Channel] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_AccountingSchemas] PRIMARY KEY ([SchemaId]),
        CONSTRAINT [FK_AccountingSchemas_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [FeeConfigurations] (
        [FeeId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NULL,
        [TransactionType] int NOT NULL,
        [FeeType] int NOT NULL,
        [FixedAmount] decimal(18,4) NOT NULL,
        [Percentage] decimal(7,4) NOT NULL,
        [MaxFeeAmount] decimal(18,4) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_FeeConfigurations] PRIMARY KEY ([FeeId]),
        CONSTRAINT [FK_FeeConfigurations_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [PartnerAccounts] (
        [AccountId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [Balance] decimal(18,4) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [LastMovementAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PartnerAccounts] PRIMARY KEY ([AccountId]),
        CONSTRAINT [FK_PartnerAccounts_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [Subscriptions] (
        [SubscriptionId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [BankAccountNumber] nvarchar(500) NOT NULL,
        [BankCode] nvarchar(20) NOT NULL,
        [PhoneNumber] nvarchar(500) NOT NULL,
        [PhoneOperator] nvarchar(50) NOT NULL,
        [Status] int NOT NULL,
        [SubscribedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([SubscriptionId]),
        CONSTRAINT [FK_Subscriptions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Subscriptions_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] uniqueidentifier NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Role] int NOT NULL,
        [PartnerId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [TwoFactorSecret] nvarchar(200) NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_Users_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [AccountingSchemaLines] (
        [LineId] uniqueidentifier NOT NULL,
        [SchemaId] uniqueidentifier NOT NULL,
        [LineOrder] int NOT NULL,
        [AccountCode] nvarchar(50) NOT NULL,
        [AccountType] int NOT NULL,
        [AccountExpression] nvarchar(500) NULL,
        [Side] int NOT NULL,
        [AmountFormula] nvarchar(500) NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [IsConditional] bit NOT NULL,
        [Condition] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AccountingSchemaLines] PRIMARY KEY ([LineId]),
        CONSTRAINT [FK_AccountingSchemaLines_AccountingSchemas_SchemaId] FOREIGN KEY ([SchemaId]) REFERENCES [AccountingSchemas] ([SchemaId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [Transactions] (
        [TransactionId] uniqueidentifier NOT NULL,
        [PartnerTransactionRef] nvarchar(100) NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [SubscriptionId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [TransactionType] int NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [FeeAmount] decimal(18,4) NOT NULL,
        [NetAmount] decimal(18,4) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Status] int NOT NULL,
        [FailureReason] nvarchar(500) NULL,
        [AccountingStatus] int NOT NULL,
        [SchemaId] uniqueidentifier NULL,
        [InitiatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [ExternalRef] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [FK_Transactions_AccountingSchemas_SchemaId] FOREIGN KEY ([SchemaId]) REFERENCES [AccountingSchemas] ([SchemaId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Transactions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Transactions_Subscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [Subscriptions] ([SubscriptionId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [TokenId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByToken] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([TokenId]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [JournalEntries] (
        [EntryId] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NOT NULL,
        [SchemaId] uniqueidentifier NOT NULL,
        [EntryDate] datetime2 NOT NULL,
        [TotalDebit] decimal(18,4) NOT NULL,
        [TotalCredit] decimal(18,4) NOT NULL,
        [IsBalanced] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([EntryId]),
        CONSTRAINT [FK_JournalEntries_AccountingSchemas_SchemaId] FOREIGN KEY ([SchemaId]) REFERENCES [AccountingSchemas] ([SchemaId]) ON DELETE CASCADE,
        CONSTRAINT [FK_JournalEntries_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([TransactionId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [PartnerAccountMovements] (
        [MovementId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NULL,
        [MovementType] int NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [BalanceBefore] decimal(18,4) NOT NULL,
        [BalanceAfter] decimal(18,4) NOT NULL,
        [MovementDate] datetime2 NOT NULL,
        [Description] nvarchar(500) NULL,
        [PartnerAccountAccountId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PartnerAccountMovements] PRIMARY KEY ([MovementId]),
        CONSTRAINT [FK_PartnerAccountMovements_PartnerAccounts_PartnerAccountAccountId] FOREIGN KEY ([PartnerAccountAccountId]) REFERENCES [PartnerAccounts] ([AccountId]),
        CONSTRAINT [FK_PartnerAccountMovements_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PartnerAccountMovements_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([TransactionId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [WebhookLogs] (
        [LogId] uniqueidentifier NOT NULL,
        [PartnerId] uniqueidentifier NOT NULL,
        [TransactionId] uniqueidentifier NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [TargetUrl] nvarchar(500) NOT NULL,
        [AttemptCount] int NOT NULL,
        [LastAttemptAt] datetime2 NULL,
        [NextAttemptAt] datetime2 NULL,
        [Status] int NOT NULL,
        [ResponseCode] int NULL,
        [ResponseBody] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_WebhookLogs] PRIMARY KEY ([LogId]),
        CONSTRAINT [FK_WebhookLogs_Partners_PartnerId] FOREIGN KEY ([PartnerId]) REFERENCES [Partners] ([PartnerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_WebhookLogs_Transactions_TransactionId] FOREIGN KEY ([TransactionId]) REFERENCES [Transactions] ([TransactionId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE TABLE [JournalLines] (
        [LineId] uniqueidentifier NOT NULL,
        [EntryId] uniqueidentifier NOT NULL,
        [AccountCode] nvarchar(50) NOT NULL,
        [Side] int NOT NULL,
        [Amount] decimal(18,4) NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JournalLines] PRIMARY KEY ([LineId]),
        CONSTRAINT [FK_JournalLines_JournalEntries_EntryId] FOREIGN KEY ([EntryId]) REFERENCES [JournalEntries] ([EntryId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccountingSchemaLines_SchemaId] ON [AccountingSchemaLines] ([SchemaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccountingSchemas_PartnerId] ON [AccountingSchemas] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccountingSchemas_TransactionType_TransactionSide_Channel_PartnerId_IsActive_Priority] ON [AccountingSchemas] ([TransactionType], [TransactionSide], [Channel], [PartnerId], [IsActive], [Priority]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_PerformedAt] ON [AuditLogs] ([PerformedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_ExternalCustomerId] ON [Customers] ([ExternalCustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_FeeConfigurations_PartnerId_TransactionType_IsActive] ON [FeeConfigurations] ([PartnerId], [TransactionType], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_EntryDate] ON [JournalEntries] ([EntryDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_SchemaId] ON [JournalEntries] ([SchemaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_TransactionId] ON [JournalEntries] ([TransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JournalLines_AccountCode] ON [JournalLines] ([AccountCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JournalLines_EntryId] ON [JournalLines] ([EntryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PartnerAccountMovements_PartnerAccountAccountId] ON [PartnerAccountMovements] ([PartnerAccountAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PartnerAccountMovements_PartnerId_MovementDate] ON [PartnerAccountMovements] ([PartnerId], [MovementDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PartnerAccountMovements_TransactionId] ON [PartnerAccountMovements] ([TransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PartnerAccounts_PartnerId] ON [PartnerAccounts] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Partners_PartnerCode] ON [Partners] ([PartnerCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Subscriptions_CustomerId_PartnerId_PhoneNumber] ON [Subscriptions] ([CustomerId], [PartnerId], [PhoneNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_PartnerId] ON [Subscriptions] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_CustomerId] ON [Transactions] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_InitiatedAt] ON [Transactions] ([InitiatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Transactions_PartnerId_PartnerTransactionRef] ON [Transactions] ([PartnerId], [PartnerTransactionRef]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_SchemaId] ON [Transactions] ([SchemaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_Status] ON [Transactions] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Transactions_SubscriptionId] ON [Transactions] ([SubscriptionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_PartnerId] ON [Users] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WebhookLogs_NextAttemptAt] ON [WebhookLogs] ([NextAttemptAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WebhookLogs_PartnerId] ON [WebhookLogs] ([PartnerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WebhookLogs_Status] ON [WebhookLogs] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WebhookLogs_TransactionId] ON [WebhookLogs] ([TransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260523150822_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260523150822_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

