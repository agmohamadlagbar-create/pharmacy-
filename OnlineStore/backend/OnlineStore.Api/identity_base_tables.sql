-- ============================================
-- PART 1: Create ASP.NET Identity base tables
-- These are missing from the EF migration script
-- ============================================

-- AspNetRoles
IF OBJECT_ID(N'[AspNetRoles]') IS NULL
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END;
GO

-- AspNetUsers (AppUser with all custom fields)
IF OBJECT_ID(N'[AspNetUsers]') IS NULL
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [AccountType] int NOT NULL,
        [FullName] nvarchar(max) NULL,
        [ProfilePictureUrl] nvarchar(max) NULL,
        [Street] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [BuildingNumber] nvarchar(max) NULL,
        [PostalCode] nvarchar(max) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Country] nvarchar(max) NULL,
        [StripeCustomerId] nvarchar(max) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
    CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
END;
GO

-- AspNetRoleClaims
IF OBJECT_ID(N'[AspNetRoleClaims]') IS NULL
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

-- AspNetUserClaims
IF OBJECT_ID(N'[AspNetUserClaims]') IS NULL
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

-- AspNetUserLogins
IF OBJECT_ID(N'[AspNetUserLogins]') IS NULL
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

-- AspNetUserRoles
IF OBJECT_ID(N'[AspNetUserRoles]') IS NULL
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

-- AspNetUserTokens
IF OBJECT_ID(N'[AspNetUserTokens]') IS NULL
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- ShopProfiles
IF OBJECT_ID(N'[ShopProfiles]') IS NULL
BEGIN
    CREATE TABLE [ShopProfiles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ShopName] nvarchar(200) NOT NULL,
        [Location] nvarchar(400) NOT NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [OpeningTime] nvarchar(max) NULL,
        [ClosingTime] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [Description] nvarchar(2000) NULL,
        [OwnerName] nvarchar(200) NULL,
        [Phone] nvarchar(50) NULL,
        [StoreType] nvarchar(max) NOT NULL DEFAULT N'',
        [RegionCode] nvarchar(max) NOT NULL DEFAULT N'',
        CONSTRAINT [PK_ShopProfiles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_ShopProfiles_UserId] ON [ShopProfiles] ([UserId]);
END;
GO

-- UserAddresses
IF OBJECT_ID(N'[UserAddresses]') IS NULL
BEGIN
    CREATE TABLE [UserAddresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Street] nvarchar(250) NULL,
        [City] nvarchar(100) NULL,
        [BuildingNumber] nvarchar(50) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_UserAddresses_UserId] ON [UserAddresses] ([UserId]);
END;
GO

-- AiConversations
IF OBJECT_ID(N'[AiConversations]') IS NULL
BEGIN
    CREATE TABLE [AiConversations] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(300) NULL,
        [ModelName] nvarchar(120) NULL,
        [SystemPrompt] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [LastMessageAtUtc] datetime2 NULL,
        [State_AwaitingConfirmation] bit NOT NULL DEFAULT 0,
        [State_PendingAction] nvarchar(100) NULL,
        [State_PendingProductId] int NULL,
        [State_PendingProductName] nvarchar(300) NULL,
        [State_PendingQuantity] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_AiConversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiConversations_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AiConversations_UserId] ON [AiConversations] ([UserId]);
    CREATE INDEX [IX_AiConversations_UserId_UpdatedAtUtc] ON [AiConversations] ([UserId], [UpdatedAtUtc]);
END;
GO

-- AiMessages
IF OBJECT_ID(N'[AiMessages]') IS NULL
BEGIN
    CREATE TABLE [AiMessages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [SequenceNumber] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AiMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AiMessages_Role] CHECK ([Role] IN ('user','assistant','system','tool')),
        CONSTRAINT [FK_AiMessages_AiConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [AiConversations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AiMessages_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AiMessages_ConversationId] ON [AiMessages] ([ConversationId]);
    CREATE INDEX [IX_AiMessages_UserId] ON [AiMessages] ([UserId]);
    CREATE UNIQUE INDEX [IX_AiMessages_ConversationId_SequenceNumber] ON [AiMessages] ([ConversationId], [SequenceNumber]);
END;
GO

-- AiToolCalls
IF OBJECT_ID(N'[AiToolCalls]') IS NULL
BEGIN
    CREATE TABLE [AiToolCalls] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [MessageId] int NULL,
        [ToolCallId] nvarchar(120) NOT NULL,
        [ToolName] nvarchar(120) NOT NULL,
        [ArgumentsJson] nvarchar(max) NULL,
        [ResultJson] nvarchar(max) NULL,
        [Status] nvarchar(30) NOT NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AiToolCalls] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AiToolCalls_Status] CHECK ([Status] IN ('Requested','Succeeded','Failed')),
        CONSTRAINT [FK_AiToolCalls_AiConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [AiConversations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AiToolCalls_AiMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [AiMessages] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_AiToolCalls_ConversationId] ON [AiToolCalls] ([ConversationId]);
    CREATE INDEX [IX_AiToolCalls_MessageId] ON [AiToolCalls] ([MessageId]);
    CREATE INDEX [IX_AiToolCalls_ConversationId_StartedAtUtc] ON [AiToolCalls] ([ConversationId], [StartedAtUtc]);
    CREATE UNIQUE INDEX [IX_AiToolCalls_ConversationId_ToolCallId] ON [AiToolCalls] ([ConversationId], [ToolCallId]);
END;
GO

PRINT 'All base tables created successfully!';
GO
