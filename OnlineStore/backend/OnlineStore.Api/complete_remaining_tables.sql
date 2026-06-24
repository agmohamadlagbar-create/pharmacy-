-- ============================================
-- Complete remaining tables + mark all migrations done
-- Run this ONCE in WebMSSQL
-- ============================================

-- Categories
IF OBJECT_ID(N'[Categories]') IS NULL
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [ImageUrl] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ParentId] int NULL,
        [Code] nvarchar(max) NOT NULL DEFAULT N'',
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
    CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
    ALTER TABLE [Categories] ADD CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]);
END;
GO

-- Products
IF OBJECT_ID(N'[Products]') IS NULL
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [StoreUserId] nvarchar(450) NOT NULL,
        [CategoryId] int NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Price] decimal(18,2) NOT NULL,
        [ImageUrl] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [RejectionReason] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [AdditionalImageUrls] nvarchar(max) NOT NULL DEFAULT N'',
        [OriginalQuantity] int NOT NULL DEFAULT 0,
        [StockQuantity] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
    CREATE INDEX [IX_Products_Status] ON [Products] ([Status]);
    CREATE INDEX [IX_Products_StoreUserId] ON [Products] ([StoreUserId]);
END;
GO

-- Orders
IF OBJECT_ID(N'[Orders]') IS NULL
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [StoreUserId] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [DeliveryAddress] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [OrderGroupId] nvarchar(100) NULL,
        [PaymentMethod] nvarchar(max) NULL,
        [OfferCode] nvarchar(max) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Orders_StoreUserId] ON [Orders] ([StoreUserId]);
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
    CREATE INDEX [IX_Orders_OrderGroupId] ON [Orders] ([OrderGroupId]);
END;
GO

-- OrderItems
IF OBJECT_ID(N'[OrderItems]') IS NULL
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;
GO

-- DeliveryProfiles
IF OBJECT_ID(N'[DeliveryProfiles]') IS NULL
BEGIN
    CREATE TABLE [DeliveryProfiles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Phone] nvarchar(50) NULL,
        [VehicleInfo] nvarchar(200) NULL,
        [IsAvailable] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_DeliveryProfiles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_DeliveryProfiles_UserId] ON [DeliveryProfiles] ([UserId]);
END;
GO

-- DeliveryJobs
IF OBJECT_ID(N'[DeliveryJobs]') IS NULL
BEGIN
    CREATE TABLE [DeliveryJobs] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [DeliveryUserId] nvarchar(450) NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [AcceptedAtUtc] datetime2 NULL,
        [DeliveredAtUtc] datetime2 NULL,
        CONSTRAINT [PK_DeliveryJobs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeliveryJobs_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_DeliveryJobs_DeliveryUserId] ON [DeliveryJobs] ([DeliveryUserId]);
    CREATE UNIQUE INDEX [IX_DeliveryJobs_OrderId] ON [DeliveryJobs] ([OrderId]);
    CREATE INDEX [IX_DeliveryJobs_Status] ON [DeliveryJobs] ([Status]);
END;
GO

-- Notifications
IF OBJECT_ID(N'[Notifications]') IS NULL
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

-- RefreshTokens
IF OBJECT_ID(N'[RefreshTokens]') IS NULL
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [RevokedUtc] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

-- Reviews
IF OBJECT_ID(N'[Reviews]') IS NULL
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [UserName] nvarchar(200) NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
    CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
END;
GO

-- AdBanners
IF OBJECT_ID(N'[AdBanners]') IS NULL
BEGIN
    CREATE TABLE [AdBanners] (
        [Id] int NOT NULL IDENTITY,
        [ImageUrl] nvarchar(500) NOT NULL,
        [Title] nvarchar(200) NULL,
        [LinkUrl] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AdBanners] PRIMARY KEY ([Id])
    );
END;
GO

-- Carts
IF OBJECT_ID(N'[Carts]') IS NULL
BEGIN
    CREATE TABLE [Carts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
END;
GO

-- CartItems
IF OBJECT_ID(N'[CartItems]') IS NULL
BEGIN
    CREATE TABLE [CartItems] (
        [Id] int NOT NULL IDENTITY,
        [CartId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [AddedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_CartItems_CartId] ON [CartItems] ([CartId]);
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END;
GO

-- ProductImages
IF OBJECT_ID(N'[ProductImages]') IS NULL
BEGIN
    CREATE TABLE [ProductImages] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ImageType] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'active',
        [SortOrder] int NOT NULL,
        [UploadedBy] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [ArchivedAt] datetime2 NULL,
        CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
    CREATE INDEX [IX_ProductImages_StorageKey] ON [ProductImages] ([StorageKey]);
END;
GO

-- UserImages
IF OBJECT_ID(N'[UserImages]') IS NULL
BEGIN
    CREATE TABLE [UserImages] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Country] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ImageType] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT N'active',
        [CreatedAt] datetime2 NOT NULL,
        [ArchivedAt] datetime2 NULL,
        CONSTRAINT [PK_UserImages] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_UserImages_StorageKey] ON [UserImages] ([StorageKey]);
    CREATE INDEX [IX_UserImages_UserId] ON [UserImages] ([UserId]);
END;
GO

-- ============================================
-- Mark ALL migrations as applied
-- ============================================
DELETE FROM [__EFMigrationsHistory];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
(N'20260304213817_AddProfilePictureUrl', N'8.0.2'),
(N'20260304223717_AddFullName', N'8.0.2'),
(N'20260304231704_AddUserAddressFields', N'8.0.2'),
(N'20260305201253_AddAuthAndBusinessEntities', N'8.0.2'),
(N'20260305224635_AddHierarchicalCategoriesAndStoreType', N'8.0.2'),
(N'20260310231656_AddCodesToEntities', N'8.0.2'),
(N'20260311042139_AddAdBanners', N'8.0.2'),
(N'20260316214105_AddProductDetails2', N'8.0.2'),
(N'20260405153932_AddPhotoStorageTables', N'8.0.2');
GO

PRINT 'All tables created and migrations marked as applied!';
GO
