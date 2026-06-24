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
    WHERE [MigrationId] = N'20260304213817_AddProfilePictureUrl'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ProfilePictureUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304213817_AddProfilePictureUrl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304213817_AddProfilePictureUrl', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304223717_AddFullName'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [FullName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304223717_AddFullName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304223717_AddFullName', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [BuildingNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [City] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Latitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Longitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PostalCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Street] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304231704_AddUserAddressFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304231704_AddUserAddressFields', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [Description] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [OwnerName] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [Phone] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [ImageUrl] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_DeliveryJobs_DeliveryUserId] ON [DeliveryJobs] ([DeliveryUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeliveryJobs_OrderId] ON [DeliveryJobs] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_DeliveryJobs_Status] ON [DeliveryJobs] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeliveryProfiles_UserId] ON [DeliveryProfiles] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Orders_StoreUserId] ON [Orders] ([StoreUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Products_Status] ON [Products] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_Products_StoreUserId] ON [Products] ([StoreUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305201253_AddAuthAndBusinessEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305201253_AddAuthAndBusinessEntities', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305224635_AddHierarchicalCategoriesAndStoreType'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [StoreType] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305224635_AddHierarchicalCategoriesAndStoreType'
)
BEGIN
    ALTER TABLE [Categories] ADD [ParentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305224635_AddHierarchicalCategoriesAndStoreType'
)
BEGIN
    CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305224635_AddHierarchicalCategoriesAndStoreType'
)
BEGIN
    ALTER TABLE [Categories] ADD CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260305224635_AddHierarchicalCategoriesAndStoreType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260305224635_AddHierarchicalCategoriesAndStoreType', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310231656_AddCodesToEntities'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [RegionCode] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310231656_AddCodesToEntities'
)
BEGIN
    ALTER TABLE [Categories] ADD [Code] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260310231656_AddCodesToEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260310231656_AddCodesToEntities', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260311042139_AddAdBanners'
)
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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260311042139_AddAdBanners'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260311042139_AddAdBanners', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    ALTER TABLE [Products] ADD [AdditionalImageUrls] nvarchar(max) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    ALTER TABLE [Products] ADD [OriginalQuantity] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    ALTER TABLE [Products] ADD [StockQuantity] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    CREATE INDEX [IX_Reviews_ProductId] ON [Reviews] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316214105_AddProductDetails2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260316214105_AddProductDetails2', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [Country] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE TABLE [Carts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_CartItems_CartId] ON [CartItems] ([CartId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_ProductImages_StorageKey] ON [ProductImages] ([StorageKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_UserImages_StorageKey] ON [UserImages] ([StorageKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    CREATE INDEX [IX_UserImages_UserId] ON [UserImages] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405153932_AddPhotoStorageTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405153932_AddPhotoStorageTables', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193703_AddShopProfileAvailability'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [ClosingTime] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193703_AddShopProfileAvailability'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [OpeningTime] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405193703_AddShopProfileAvailability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405193703_AddShopProfileAvailability', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406203959_AddUserAddresses'
)
BEGIN
    ALTER TABLE [Orders] ADD [OfferCode] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406203959_AddUserAddresses'
)
BEGIN
    ALTER TABLE [Orders] ADD [PaymentMethod] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406203959_AddUserAddresses'
)
BEGIN
    CREATE TABLE [UserAddresses] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Street] nvarchar(250) NULL,
        [BuildingNumber] nvarchar(50) NULL,
        [City] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406203959_AddUserAddresses'
)
BEGIN
    CREATE INDEX [IX_UserAddresses_UserId] ON [UserAddresses] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406203959_AddUserAddresses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406203959_AddUserAddresses', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406220501_AddStripeCustomerIdToAppUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [StripeCustomerId] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260406220501_AddStripeCustomerIdToAppUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260406220501_AddStripeCustomerIdToAppUser', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414051405_MakeOrderItemProductIdNullable'
)
BEGIN
    ALTER TABLE [OrderItems] DROP CONSTRAINT [FK_OrderItems_Products_ProductId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414051405_MakeOrderItemProductIdNullable'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrderItems]') AND [c].[name] = N'ProductId');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [OrderItems] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [OrderItems] ALTER COLUMN [ProductId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414051405_MakeOrderItemProductIdNullable'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260414051405_MakeOrderItemProductIdNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260414051405_MakeOrderItemProductIdNullable', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418021058_DropOrderTrackingTable'
)
BEGIN
    IF OBJECT_ID('OrderTrackings', 'U') IS NOT NULL DROP TABLE [OrderTrackings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260418021058_DropOrderTrackingTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260418021058_DropOrderTrackingTable', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430202327_AddOrderGroupId'
)
BEGIN
    ALTER TABLE [Orders] ADD [OrderGroupId] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430202327_AddOrderGroupId'
)
BEGIN
    CREATE INDEX [IX_Orders_OrderGroupId] ON [Orders] ([OrderGroupId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430202327_AddOrderGroupId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430202327_AddOrderGroupId', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [Latitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    ALTER TABLE [ShopProfiles] ADD [Longitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    CREATE TABLE [AiConversations] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(300) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AiConversations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    CREATE TABLE [AiMessages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AiMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiMessages_AiConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [AiConversations] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    CREATE INDEX [IX_AiConversations_UserId] ON [AiConversations] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    CREATE INDEX [IX_AiMessages_ConversationId] ON [AiMessages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512130104_AddAiConversationMemory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512130104_AddAiConversationMemory', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiMessages] ADD [SequenceNumber] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiMessages] ADD [UserId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [LastMessageAtUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [ModelName] nvarchar(120) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [SystemPrompt] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
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
        CONSTRAINT [FK_AiToolCalls_AiMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [AiMessages] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiMessages_ConversationId_SequenceNumber] ON [AiMessages] ([ConversationId], [SequenceNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE INDEX [IX_AiMessages_UserId] ON [AiMessages] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    EXEC(N'ALTER TABLE [AiMessages] ADD CONSTRAINT [CK_AiMessages_Role] CHECK ([Role] IN (''user'',''assistant'',''system'',''tool''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE INDEX [IX_AiConversations_UserId_UpdatedAtUtc] ON [AiConversations] ([UserId], [UpdatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE INDEX [IX_AiToolCalls_ConversationId] ON [AiToolCalls] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE INDEX [IX_AiToolCalls_ConversationId_StartedAtUtc] ON [AiToolCalls] ([ConversationId], [StartedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AiToolCalls_ConversationId_ToolCallId] ON [AiToolCalls] ([ConversationId], [ToolCallId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    CREATE INDEX [IX_AiToolCalls_MessageId] ON [AiToolCalls] ([MessageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiConversations] ADD CONSTRAINT [FK_AiConversations_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    ALTER TABLE [AiMessages] ADD CONSTRAINT [FK_AiMessages_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513030335_AddAiConversationToolCalls'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513030335_AddAiConversationToolCalls', N'8.0.2');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [State_AwaitingConfirmation] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [State_PendingAction] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [State_PendingProductId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [State_PendingProductName] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    ALTER TABLE [AiConversations] ADD [State_PendingQuantity] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260513043539_AddAiConversationState'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260513043539_AddAiConversationState', N'8.0.2');
END;
GO

COMMIT;
GO

