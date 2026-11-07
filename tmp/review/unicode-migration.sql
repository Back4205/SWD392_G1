BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouse]') AND [c].[name] = N'WarehouseName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Warehouse] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Warehouse] ALTER COLUMN [WarehouseName] nvarchar(100) NOT NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Warehouse]') AND [c].[name] = N'Address');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Warehouse] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Warehouse] ALTER COLUMN [Address] nvarchar(255) NOT NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[User]') AND [c].[name] = N'FullName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [User] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [User] ALTER COLUMN [FullName] nvarchar(100) NOT NULL;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Supplier]') AND [c].[name] = N'SupplierName');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Supplier] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Supplier] ALTER COLUMN [SupplierName] nvarchar(100) NOT NULL;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Supplier]') AND [c].[name] = N'ContactPerson');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Supplier] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Supplier] ALTER COLUMN [ContactPerson] nvarchar(100) NOT NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Supplier]') AND [c].[name] = N'Address');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Supplier] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Supplier] ALTER COLUMN [Address] nvarchar(255) NULL;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StorageSectionType]') AND [c].[name] = N'Description');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [StorageSectionType] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [StorageSectionType] ALTER COLUMN [Description] nvarchar(255) NULL;
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StorageSection]') AND [c].[name] = N'SectionName');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [StorageSection] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [StorageSection] ALTER COLUMN [SectionName] nvarchar(100) NOT NULL;
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StorageBin]') AND [c].[name] = N'BinName');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [StorageBin] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [StorageBin] ALTER COLUMN [BinName] nvarchar(100) NOT NULL;
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Role]') AND [c].[name] = N'Description');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Role] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Role] ALTER COLUMN [Description] nvarchar(255) NULL;
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductVariant]') AND [c].[name] = N'VariantName');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ProductVariant] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [ProductVariant] ALTER COLUMN [VariantName] nvarchar(100) NULL;
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProductVariant]') AND [c].[name] = N'Specs');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ProductVariant] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [ProductVariant] ALTER COLUMN [Specs] nvarchar(500) NULL;
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Product]') AND [c].[name] = N'ProductName');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Product] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Product] ALTER COLUMN [ProductName] nvarchar(100) NOT NULL;
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Permission]') AND [c].[name] = N'Description');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Permission] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Permission] ALTER COLUMN [Description] nvarchar(255) NULL;
GO

DROP INDEX [IX_Category_CategoryName] ON [Category];
DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Category]') AND [c].[name] = N'CategoryName');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Category] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [Category] ALTER COLUMN [CategoryName] nvarchar(100) NOT NULL;
CREATE UNIQUE INDEX [IX_Category_CategoryName] ON [Category] ([CategoryName]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261107081558_PreserveVietnameseText', N'8.0.33');
GO

COMMIT;
GO

