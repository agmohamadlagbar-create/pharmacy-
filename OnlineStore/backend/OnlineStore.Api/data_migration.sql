-- Data Migration Script from Local to Production
-- Generated 2026-05-19 07:02:14

-- Disable FK constraints during import
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- AspNetRoles (4 rows)
DELETE FROM [AspNetRoles];
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'1e9f1a62-3b41-4e65-aa51-ba04fa9e566b', N'Admin', N'ADMIN', NULL);
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'83ce884c-8607-447f-a054-54c81d185301', N'Shop', N'SHOP', NULL);
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'bcb1fce2-d5e4-4f4f-81a0-46ff7acfbca0', N'Delivery', N'DELIVERY', NULL);
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp]) VALUES (N'f0ef94da-5ea2-4297-9a66-5a2071fd4948', N'User', N'USER', NULL);
GO

-- AspNetUsers (6 rows)
DELETE FROM [AspNetUsers];
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', 2, N'oopo39134@gmail.com', N'OOPO39134@GMAIL.COM', N'oopo39134@gmail.com', N'OOPO39134@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEJGNbpan9xG18NapCFzEA0TtrbGljUx1A9rW2ZzRgYObHhMFuaum/6FiK8Uei2kL7w==', N'SY5EX2DU6TKJ3X3QU2P7MYESEN3VIFZK', N'a98c9073-26fa-4b7b-bdd3-1d3563a5c1cf', NULL, 0, 0, NULL, 1, 0, NULL, N'delivery test ', N'', N'Karabük', 41.2181264150243, 32.6663208755703, N'78020', N'1003. Cadde', N'Turkey', NULL);
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', 3, N'admin@lagbar.com', N'ADMIN@LAGBAR.COM', N'admin@lagbar.com', N'ADMIN@LAGBAR.COM', 1, N'AQAAAAIAAYagAAAAEE2LtjdXonyJj4Wf3kQYIgcbaYkGADcD/Gn/17gPCcp8DFzfwAsK7KOBe4JPKVFnEA==', N'43X4ORERKS7Q6YTA6PPJHLVGPAPSS6SA', N'1d20afbe-b4b0-496c-9897-f2704bc64d18', NULL, 0, 0, NULL, 1, 0, N'/media/Turkey/KarabkMerkez/users/4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e/processed/medium_Screenshot 2025-10-16 024334.webp', N'LAGBAR Admin', N'', N'Ümraniye', 41.0148273519778, 29.1127250697128, N'34760', N'Sırt Sokağı', N'Turkey', NULL);
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 1, N'lagbarshop@gmail.com', N'LAGBARSHOP@GMAIL.COM', N'lagbarshop@gmail.com', N'LAGBARSHOP@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEM/eey9R9wXecfeJoxsqvBQfSTqUlATt8DkPNEhMGpYkHAC+DWcX66FN5phjrLR3iw==', N'URFVUQ7CX3TCPDCYDQ3PQBHDW3RCAWIE', N'68ef5c75-baaa-4741-8c6c-aad286843fb4', NULL, 0, 0, NULL, 1, 0, N'/media/Turkey/KarabkMerkez/users/7077a878-a3fe-4fd4-81f5-6cb7762ce311/avatar/original/Screenshot_2025-10-16_021627_20260405_163534.png', N'LAGBAR SHOP', N'', N'Karabük', 41.2144656236906, 32.6628822378092, N'78050', N'Kılavuzlar Mahallesi', N'Turkey', NULL);
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'96c78f83-df5e-4a81-a30c-f9d391831578', 0, N'muhammed0949857914@gmail.com', N'MUHAMMED0949857914@GMAIL.COM', N'muhammed0949857914@gmail.com', N'MUHAMMED0949857914@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEPx8l9bGwMqFigmrObuPM4YzzyoOkoXnhFqIlYUz/KQYRXyCPM8NQl9mSKTKlCljWA==', N'L5R4QSQ5AMZEMMJHA23TSU5IORR4N7EU', N'dee9fbac-d826-47dc-ad1a-be59e64a7761', NULL, 0, 0, NULL, 1, 0, N'/media/Turkey/KarabkMerkez/users/96c78f83-df5e-4a81-a30c-f9d391831578/avatar/original/Screenshot_2025-10-16_024334_20260405_163010.png', N'Mohamad Lagbar', N'2', N'Karabük', 41.1975277658358, 32.6300259412138, N'78200', N'Öğlebeli', N'Turkey', N'cus_UHvc8XDgwuElv2');
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'bb136836-8481-4635-a33e-b1bca36e1c22', 1, N'ag.mohamadlagbar@gmail.com', N'AG.MOHAMADLAGBAR@GMAIL.COM', N'ag.mohamadlagbar@gmail.com', N'AG.MOHAMADLAGBAR@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEGMUnWpOmJjOqmDU2pIQRUpgueiHiMcN/eUXNcybxmMVBsKslzeS5DxVBecGnDhQOg==', N'KCW6L65WEHZA4NDUDHYXUBG7VPHOXPIU', N'181c7f13-46a8-4c36-bfc5-1534942c07f8', NULL, 0, 0, NULL, 1, 0, NULL, N'MUHAMMED', N'2', N'Karabük', 41.2019579944579, 32.6433527397655, N'78200', N'Kılavuzlar Mahallesi', N'Turkey', NULL);
INSERT INTO [AspNetUsers] ([Id], [AccountType], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [ProfilePictureUrl], [FullName], [BuildingNumber], [City], [Latitude], [Longitude], [PostalCode], [Street], [Country], [StripeCustomerId]) VALUES (N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', 0, N'u0912642@gmail.com', N'U0912642@GMAIL.COM', N'u0912642@gmail.com', N'U0912642@GMAIL.COM', 1, N'AQAAAAIAAYagAAAAEGCHS3jaWyKhb3KeC+zdYYwAf+wWq9JhEdIJr19TO/eopJbef6Z8oUiIYfdA8eNs/g==', N'I67GMZ2FMMTDQJQEWHCOAU4L2OJXMGLB', N'0083d7de-fd13-4e8f-b6ce-f3aaee2e641c', NULL, 0, 0, NULL, 1, 0, NULL, N'MUHAMMED', N'2', N'Karabük', 41.2153308026532, 32.6679627312359, N'78050', N'Kılavuzlar Mahallesi', N'Turkey', NULL);
GO

-- AspNetUserRoles (6 rows)
DELETE FROM [AspNetUserRoles];
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', N'1e9f1a62-3b41-4e65-aa51-ba04fa9e566b');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', N'83ce884c-8607-447f-a054-54c81d185301');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'bb136836-8481-4635-a33e-b1bca36e1c22', N'83ce884c-8607-447f-a054-54c81d185301');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', N'bcb1fce2-d5e4-4f4f-81a0-46ff7acfbca0');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'96c78f83-df5e-4a81-a30c-f9d391831578', N'f0ef94da-5ea2-4297-9a66-5a2071fd4948');
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES (N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'f0ef94da-5ea2-4297-9a66-5a2071fd4948');
GO

-- ShopProfiles (1 rows)
SET IDENTITY_INSERT [ShopProfiles] ON;
DELETE FROM [ShopProfiles];
INSERT INTO [ShopProfiles] ([Id], [UserId], [ShopName], [Location], [CreatedAtUtc], [Description], [OwnerName], [Phone], [StoreType], [RegionCode], [ClosingTime], [OpeningTime], [Latitude], [Longitude]) VALUES (1, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', N'Lagbar', N'Türkiye', N'2026-03-05T20:25:10.1609157', N'ALL WHAT YOU WANT IN ONE PLACE', NULL, NULL, N'', N'', N'23:59', N'00:00', NULL, NULL);
SET IDENTITY_INSERT [ShopProfiles] OFF;
GO

-- DeliveryProfiles (1 rows)
SET IDENTITY_INSERT [DeliveryProfiles] ON;
DELETE FROM [DeliveryProfiles];
INSERT INTO [DeliveryProfiles] ([Id], [UserId], [Phone], [VehicleInfo], [IsAvailable], [CreatedAtUtc]) VALUES (1, N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', NULL, N'motorcycle ', 1, N'2026-04-07T01:29:49.6835028');
SET IDENTITY_INSERT [DeliveryProfiles] OFF;
GO

-- Categories (7 rows)
SET IDENTITY_INSERT [Categories] ON;
DELETE FROM [Categories];
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (1, N'Food', N'/media/Unknown/Unknown/categories/Screenshot_2025-11-19_195110_20260411_041349.png', N'2026-03-05T20:50:32.9133312', NULL, N'FOOD');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (2, N'Beverages', N'/media/Unknown/Unknown/categories/Screenshot_2025-10-11_051649_20260411_041307.png', N'2026-03-05T20:50:32.9134363', NULL, N'BEVERAGES');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (3, N'Cleaning Supplies', N'/media/Unknown/Unknown/categories/Screenshot_2025-11-10_094032_20260411_041317.png', N'2026-03-05T20:50:32.9134367', NULL, N'CLEANING_SUPPLIES');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (4, N'House Needs', N'/media/Unknown/Unknown/categories/Screenshot_2025-10-16_021627_20260411_041357.png', N'2026-03-05T20:50:32.9134368', NULL, N'HOUSE_NEEDS');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (5, N'Electronics', N'/media/Unknown/Unknown/categories/Screenshot_2025-11-14_055829_20260411_041337.png', N'2026-03-05T20:50:32.9134369', NULL, N'ELECTRONICS');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (6, N'Clothing', N'/media/Unknown/Unknown/categories/Screenshot_2025-10-16_024432_20260411_041324.png', N'2026-03-05T20:50:32.9134370', NULL, N'CLOTHING');
INSERT INTO [Categories] ([Id], [Name], [ImageUrl], [CreatedAtUtc], [ParentId], [Code]) VALUES (7, N'launch', NULL, N'2026-03-10T23:42:28.0707795', 1, N'LAUNCH');
SET IDENTITY_INSERT [Categories] OFF;
GO

-- Products (23 rows)
SET IDENTITY_INSERT [Products] ON;
DELETE FROM [Products];
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (1, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 1, N'test', N'test', 50.00, N'/media/Unknown/Unknown/products/0/original/Screenshot_2025-07-20_062831_20260405_155404.png', 1, NULL, N'2026-03-05T20:58:39.4306690', N'2026-04-05T15:54:06.4298643', N'[]', 0, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (2, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 1, N'test', N'test', 2.00, N'/media/Unknown/Unknown/products/0/original/Screenshot_2025-07-20_062831_20260405_155354.png', 1, NULL, N'2026-03-05T22:05:38.7426105', N'2026-04-05T15:53:55.9180091', N'[]', 0, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (3, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 1, N'test', N'test', 20.00, N'/media/Unknown/Unknown/products/0/original/d1429e95-1459-48d2-9e7b-2cf3589b3f58.jpg', 2, N'not valid ', N'2026-03-10T23:25:28.5589419', N'2026-03-10T23:26:45.6074137', N'[]', 0, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (4, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 1, N'test', N'test', 0.01, N'/media/Unknown/Unknown/products/0/original/726e37dd-fd34-4491-9142-e505dd4b2b89.png', 1, NULL, N'2026-03-10T23:46:31.5790456', N'2026-03-10T23:47:06.1539217', N'[]', 0, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (7, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 5, N'photo test', N'photo test', 20.00, N'/media/Unknown/Unknown/products/0/original/2d882131-c321-4d21-bd4c-7e1437d53979.png', 1, NULL, N'2026-03-16T22:30:11.4103525', N'2026-03-16T22:30:33.1378078', N'[]', 0, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (8, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, N'test', N'test', 2.00, N'/media/Unknown/Unknown/products/0/original/f81a5a60-e876-4df6-85b1-faad4df40d31.png', 1, NULL, N'2026-03-16T22:46:23.3527308', N'2026-04-05T16:40:51.5267996', N'["/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-16_024432_20260405_164047.png"]', 20, 20);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (10, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'TEST', NULL, 3.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051649_20260405_195117.png', 1, NULL, N'2026-04-05T19:51:21.1615464', N'2026-04-05T19:52:17.7070585', N'[]', 20, 20);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (11, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'TR', NULL, 34.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-16_024334_20260405_195145.png', 1, NULL, N'2026-04-05T19:51:47.1020662', N'2026-04-05T19:52:17.0125409', N'[]', 10, 10);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (13, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'scroll test', N'nnnn', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-16_024334_20260414_064612.png', 1, NULL, N'2026-04-14T06:46:23.2152023', N'2026-04-14T06:46:23.2152513', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (14, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'sc', N'rrr', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051710_20260414_064637.png', 1, NULL, N'2026-04-14T06:46:41.5744888', N'2026-04-14T06:46:41.5744889', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (15, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051710_20260414_064654.png', 1, NULL, N'2026-04-14T06:46:59.6151233', N'2026-04-14T06:46:59.6151236', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (16, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051649_20260414_064746.png', 1, NULL, N'2026-04-14T06:47:52.4180129', N'2026-04-14T06:47:52.4180131', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (17, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051710_20260414_064802.png', 1, NULL, N'2026-04-14T06:48:06.5728572', N'2026-04-14T06:48:06.5728574', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (18, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-16_024334_20260414_064817.png', 1, NULL, N'2026-04-14T06:48:21.3230779', N'2026-04-14T06:48:21.3230780', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (19, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'6', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051710_20260414_064829.png', 1, NULL, N'2026-04-14T06:48:37.6701807', N'2026-04-14T06:48:37.6701809', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (20, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-16_024432_20260414_064848.png', 1, NULL, N'2026-04-14T06:48:53.3436311', N'2026-04-14T06:48:53.3436315', N'[]', 5, 5);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (21, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051710_20260414_064929.png', 1, NULL, N'2026-04-14T06:49:38.6092769', N'2026-05-12T12:09:25.8341937', N'[]', 5, 4);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (22, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'5', N'5', 5.00, N'/media/Turkey/KarabkMerkez/products/0/original/Screenshot_2025-10-11_051649_20260414_064948.png', 1, NULL, N'2026-04-14T06:49:52.6367654', N'2026-05-04T20:32:39.0865527', N'[]', 5, 0);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (23, N'bb136836-8481-4635-a33e-b1bca36e1c22', 7, N'new ', N'10', 10.00, N'/media/Trkiye/Unknown/products/0/original/Screenshot_2025-10-16_024432_20260414_090310.png', 1, NULL, N'2026-04-14T09:03:16.8273811', N'2026-04-14T09:03:31.7588370', N'[]', 4, 4);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (24, N'bb136836-8481-4635-a33e-b1bca36e1c22', 2, N'd store', N'55', 50.00, N'/media/Turkey/Karabk/products/0/original/Screenshot_2025-11-10_120333_20260420_135701.png', 1, NULL, N'2026-04-20T13:57:03.3450556', N'2026-05-17T03:14:39.6566225', N'["/media/Turkey/Karabk/products/0/original/Screenshot_2025-11-10_120501_20260420_135701.png"]', 10, 9);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (25, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'apple', N'fresh apple ', 2.00, N'/media/Turkey/Mraniye/products/0/original/OIP_20260513_064754.jpg', 1, NULL, N'2026-05-13T06:48:08.4529977', N'2026-05-17T03:14:39.6564916', N'[]', 10, 9);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (26, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'apple', N'fresh apple', 5.00, N'/media/Turkey/Mraniye/products/0/original/OIP_20260513_064825.jpg', 1, NULL, N'2026-05-13T06:48:42.6770830', N'2026-05-13T06:48:42.6770831', N'[]', 20, 20);
INSERT INTO [Products] ([Id], [StoreUserId], [CategoryId], [Name], [Description], [Price], [ImageUrl], [Status], [RejectionReason], [CreatedAtUtc], [UpdatedAtUtc], [AdditionalImageUrls], [OriginalQuantity], [StockQuantity]) VALUES (27, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 2, N'تفاح', NULL, 1.00, N'/media/Turkey/Mraniye/products/0/original/OIP_20260513_074426.jpg', 1, NULL, N'2026-05-13T07:44:36.2851021', N'2026-05-17T04:31:36.0880883', N'[]', 10, 9);
SET IDENTITY_INSERT [Products] OFF;
GO

-- AdBanners (2 rows)
SET IDENTITY_INSERT [AdBanners] ON;
DELETE FROM [AdBanners];
INSERT INTO [AdBanners] ([Id], [ImageUrl], [Title], [LinkUrl], [SortOrder], [CreatedAtUtc]) VALUES (15, N'/media/Unknown/Unknown/ads/processed/large_Screenshot 2025-07-20 062831.webp', N'car', N'http://google.com', 1, N'2026-04-11T05:18:26.3480404');
INSERT INTO [AdBanners] ([Id], [ImageUrl], [Title], [LinkUrl], [SortOrder], [CreatedAtUtc]) VALUES (16, N'/media/Unknown/Unknown/ads/processed/large_Screenshot 2025-10-11 051649.webp', NULL, NULL, 0, N'2026-04-11T05:18:40.2902666');
SET IDENTITY_INSERT [AdBanners] OFF;
GO

-- Orders (30 rows)
SET IDENTITY_INSERT [Orders] ON;
DELETE FROM [Orders];
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (1, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 84.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-06T21:10:13.9040247', N'2026-04-18T14:23:47.1716560', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (2, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 84.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-06T21:12:38.8410513', N'2026-04-18T14:23:45.6143619', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (3, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 84.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-06T21:27:14.8451820', N'2026-04-18T14:23:41.3113640', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (7, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 68.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-09T04:06:15.0477572', N'2026-04-18T14:23:40.3090401', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (8, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 37.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-09T04:07:30.6106397', N'2026-04-18T14:23:37.4479747', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (9, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'0', 6, 50.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-09T05:22:30.6317529', N'2026-04-18T14:23:35.2073166', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (10, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 68.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-11T01:54:46.7692409', N'2026-04-18T14:23:27.9187264', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (11, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 34.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-11T01:55:09.2275250', N'2026-04-18T14:23:22.8322607', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (12, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 34.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-11T20:18:16.1116201', N'2026-04-18T14:21:41.1778647', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (13, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 34.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-12T21:53:04.7874569', N'2026-04-18T14:16:18.6440774', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (14, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'bb136836-8481-4635-a33e-b1bca36e1c22', 6, 20.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-14T09:33:59.6534121', N'2026-04-18T14:16:15.2859408', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (15, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 10.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-14T09:33:59.6538118', N'2026-04-18T14:16:14.0709117', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (17, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-17T03:39:24.7234038', N'2026-04-18T14:16:05.3535422', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (38, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-18T14:10:02.9734605', N'2026-04-18T14:16:03.1041350', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (39, N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: 1003. Cadde, , Karabük 78020', N'2026-04-18T14:12:36.0571292', N'2026-04-18T14:45:43.1257969', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (40, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: 1003. Cadde, 2, Karabük Merkez 78020', N'2026-04-18T14:13:12.5102920', N'2026-04-18T14:15:56.8375902', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (41, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'HOME: Kılavuzlar Mahallesi, 2, Karabük', N'2026-04-18T15:21:35.1837536', N'2026-04-18T15:21:48.9028098', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (43, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: Fevzipaşa Caddesi, 2, Istanbul 34083', N'2026-04-18T15:30:08.9626499', N'2026-04-18T15:30:24.1576863', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (44, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'Home: Fevzipaşa Caddesi, 2, Istanbul 34083', N'2026-04-18T18:11:04.8765378', N'2026-04-18T18:11:14.8147505', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (45, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 15.00, N'office: pinar mah., 2, Karabük', N'2026-04-18T18:35:06.4771841', N'2026-04-18T18:35:16.0535048', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (50, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'home: ggg, 2, Karabük', N'2026-04-20T13:16:54.4116567', N'2026-04-20T13:17:09.9522098', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (51, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'home: ggg, 2, Karabük', N'2026-04-20T13:19:20.6674379', N'2026-04-20T13:19:28.3054100', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (52, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'home: ggg, 2, Karabük', N'2026-04-20T13:21:03.6821080', N'2026-04-20T13:21:40.7938879', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (53, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'home: ggg, 2, Karabük', N'2026-04-20T13:22:13.8717508', N'2026-04-20T13:34:59.2443093', N'', N'Credit Card', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (54, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 5.00, N'home: ggg, 2, Karabük', N'2026-04-20T13:22:27.5920493', N'2026-04-20T13:22:38.9743736', N'', N'Cash on Delivery', NULL);
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (64, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 5, 5.00, N'home: ggg, 2, Karabük', N'2026-05-12T12:09:25.8338303', N'2026-05-12T12:11:05.5125990', N'', N'Credit Card', N'G-5F946570');
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (65, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 5, 3.00, N'home: ggg, 2, Karabük', N'2026-05-17T03:14:39.6554366', N'2026-05-17T03:17:04.9333453', N'', N'Credit Card', N'G-C54DAC80');
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (66, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'bb136836-8481-4635-a33e-b1bca36e1c22', 5, 50.00, N'home: ggg, 2, Karabük', N'2026-05-17T03:14:39.6566146', N'2026-05-17T03:17:04.9333456', N'', N'Credit Card', N'G-C54DAC80');
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (67, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 1.00, N'home: ggg, 2, Karabük', N'2026-05-17T04:22:19.3889100', N'2026-05-17T04:31:01.1706566', N'', N'Cash on Delivery', N'G-627C9FD8');
INSERT INTO [Orders] ([Id], [UserId], [StoreUserId], [Status], [TotalAmount], [DeliveryAddress], [CreatedAtUtc], [UpdatedAtUtc], [OfferCode], [PaymentMethod], [OrderGroupId]) VALUES (68, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', 6, 1.00, N'home: ggg, 2, Karabük', N'2026-05-17T04:31:17.9253488', N'2026-05-17T04:31:36.0880896', N'', N'Cash on Delivery', N'G-F8654262');
SET IDENTITY_INSERT [Orders] OFF;
GO

-- OrderItems (35 rows)
SET IDENTITY_INSERT [OrderItems] ON;
DELETE FROM [OrderItems];
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (1, 1, 1, 1, 50.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (2, 1, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (3, 2, 1, 1, 50.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (4, 2, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (5, 3, 1, 1, 50.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (6, 3, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (10, 7, 11, 2, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (11, 8, 10, 1, 3.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (12, 8, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (13, 9, NULL, 1, 50.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (14, 10, 11, 2, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (15, 11, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (16, 12, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (17, 13, 11, 1, 34.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (18, 14, 23, 2, 10.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (19, 15, 22, 2, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (21, 17, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (42, 38, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (43, 39, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (44, 40, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (45, 41, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (47, 43, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (48, 44, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (49, 45, 22, 3, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (54, 50, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (55, 51, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (56, 52, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (57, 53, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (58, 54, 22, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (68, 64, 21, 1, 5.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (69, 65, 27, 1, 1.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (70, 65, 25, 1, 2.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (71, 66, 24, 1, 50.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (72, 67, 27, 1, 1.00);
INSERT INTO [OrderItems] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice]) VALUES (73, 68, 27, 1, 1.00);
SET IDENTITY_INSERT [OrderItems] OFF;
GO

-- Reviews (9 rows)
SET IDENTITY_INSERT [Reviews] ON;
DELETE FROM [Reviews];
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (1, 7, N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', N'LAGBAR Admin', 2, N'test', N'2026-03-16T22:53:02.5284064');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (2, 7, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', N'lagbarshop@gmail.com', 5, N'test
', N'2026-03-16T22:53:43.5813527');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (3, 7, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar ', 2, N'test
', N'2026-03-16T22:54:51.2479084');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (4, 8, N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', N'LAGBAR Admin', 5, N'test', N'2026-03-17T21:18:52.7379884');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (5, 11, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar ', 5, N'good', N'2026-04-09T04:49:43.8592468');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (6, 11, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar ', 5, N'goof', N'2026-04-09T04:54:08.3123486');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (7, 11, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar ', 1, N'w', N'2026-04-09T04:54:31.7201576');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (8, 11, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar ', 4, N'd', N'2026-04-09T05:01:00.5306648');
INSERT INTO [Reviews] ([Id], [ProductId], [UserId], [UserName], [Rating], [Comment], [CreatedAtUtc]) VALUES (9, 24, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Mohamad Lagbar', 3, N'vood', N'2026-04-21T16:12:41.8096885');
SET IDENTITY_INSERT [Reviews] OFF;
GO

-- Carts (6 rows)
SET IDENTITY_INSERT [Carts] ON;
DELETE FROM [Carts];
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (1, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'2026-05-18T15:19:32.8643988');
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (2, N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', N'2026-04-11T21:31:31.9290143');
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (3, N'7077a878-a3fe-4fd4-81f5-6cb7762ce311', N'2026-04-11T21:31:04.9929723');
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (4, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'2026-04-20T13:14:03.9116872');
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (5, N'bb136836-8481-4635-a33e-b1bca36e1c22', N'2026-04-20T13:57:45.4397110');
INSERT INTO [Carts] ([Id], [UserId], [UpdatedAtUtc]) VALUES (6, N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', N'2026-04-18T18:14:09.9980957');
SET IDENTITY_INSERT [Carts] OFF;
GO

-- CartItems (2 rows)
SET IDENTITY_INSERT [CartItems] ON;
DELETE FROM [CartItems];
INSERT INTO [CartItems] ([Id], [CartId], [ProductId], [Quantity], [AddedAtUtc]) VALUES (70, 5, 24, 1, N'2026-04-20T13:57:32.4224753');
INSERT INTO [CartItems] ([Id], [CartId], [ProductId], [Quantity], [AddedAtUtc]) VALUES (71, 5, 22, 1, N'2026-04-20T13:57:45.4397105');
SET IDENTITY_INSERT [CartItems] OFF;
GO

-- UserAddresses (9 rows)
SET IDENTITY_INSERT [UserAddresses] ON;
DELETE FROM [UserAddresses];
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (1, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'Home', N'1003. Cadde', N'2', N'Karabük Merkez', N'78020', 41.2183861, 32.6691955, 0, N'2026-04-06T21:09:47.5896395');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (2, N'4ebcd011-a2be-4ad1-a5f4-3e9849c88e6e', N'Home', N'1003. Cadde', N'', N'Karabük Merkez', N'78020', 41.0082, 28.9784, 1, N'2026-04-11T05:21:06.4546567');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (3, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'Home', N'Fevzipaşa Caddesi', N'2', N'Istanbul', N'34083', 41.0187389045747, 28.9485331045425, 0, N'2026-04-17T03:33:29.2107653');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (4, N'e9bd3ea6-8020-45c9-9e48-a58b65ba2d91', N'HOME', N'Kılavuzlar Mahallesi', N'2', N'Karabük', N'', NULL, NULL, 1, N'2026-04-17T05:30:06.8683627');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (5, N'1def77dc-fdce-4c1f-99e6-42f1ef272a2a', N'Home', N'1003. Cadde', N'', N'Karabük', N'78020', 41.2181264150243, 32.6663208755703, 1, N'2026-04-18T14:12:33.7563778');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (6, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'office', N'pinar mah.', N'2', N'Karabük', N'', NULL, NULL, 0, N'2026-04-18T18:15:11.9860784');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (7, N'96c78f83-df5e-4a81-a30c-f9d391831578', N't', N't', N'2', N'istanbul', N'', NULL, NULL, 0, N'2026-04-18T18:36:17.8697741');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (8, N'96c78f83-df5e-4a81-a30c-f9d391831578', N'home', N'ggg', N'2', N'Karabük', N'', NULL, NULL, 1, N'2026-04-18T19:36:54.3867665');
INSERT INTO [UserAddresses] ([Id], [UserId], [Title], [Street], [BuildingNumber], [City], [PostalCode], [Latitude], [Longitude], [IsDefault], [CreatedAtUtc]) VALUES (9, N'bb136836-8481-4635-a33e-b1bca36e1c22', N'Home', N'Kılavuzlar Mahallesi', N'2', N'Karabük', N'78200', 41.2019579944579, 32.6433527397655, 1, N'2026-04-20T13:57:49.0825887');
SET IDENTITY_INSERT [UserAddresses] OFF;
GO

-- Re-enable FK constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL';
GO

