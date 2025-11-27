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
CREATE TABLE [Degrees] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [DepartmentId] int NOT NULL,
    [ManagerId] int NOT NULL,
    CONSTRAINT [PK_Degrees] PRIMARY KEY ([Id])
);

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [DegreeId] int NOT NULL,
    [ProgramId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [MessageText] nvarchar(max) NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [UPermissionsId] int NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id])
);

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ProgramId] int NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Programs] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Programs] PRIMARY KEY ([Id])
);

CREATE TABLE [Statuses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Statuses] PRIMARY KEY ([Id])
);

CREATE TABLE [UserPermissions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [StatusId] int NOT NULL,
    [RequestDate] datetime2 NOT NULL,
    [ResponseDate] datetime2 NOT NULL,
    CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [departmentId] int NOT NULL,
    [degreeId] int NOT NULL,
    [IsAdmin] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250506080900_InitialCreate', N'9.0.4');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'departmentId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Users] DROP COLUMN [departmentId];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250506082645_UpdateUserModel', N'9.0.4');

EXEC sp_rename N'[Users].[degreeId]', N'DegreeId', 'COLUMN';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250506104131_UpdateUserModelV2', N'9.0.4');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'DegreeId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Messages] DROP COLUMN [DegreeId];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'UPermissionsId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Messages] ALTER COLUMN [UPermissionsId] int NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'ProgramId');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Messages] ALTER COLUMN [ProgramId] int NULL;

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'PermissionId');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Messages] ALTER COLUMN [PermissionId] int NULL;

ALTER TABLE [Messages] ADD [DeviceId] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250507105358_UpdateDataStructure', N'9.0.4');

CREATE TABLE [Devices] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Devices] PRIMARY KEY ([Id])
);

CREATE TABLE [MessageLinks] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationId] int NOT NULL,
    [MessageId] int NOT NULL,
    [DegreeId] int NOT NULL,
    CONSTRAINT [PK_MessageLinks] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250507110934_UpdateDataStructureV2', N'9.0.4');

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'MessageText');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Messages] ALTER COLUMN [MessageText] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250507113906_UpdateDataStructureV3', N'9.0.4');

ALTER TABLE [Programs] ADD [ProducentId] int NULL;

ALTER TABLE [Devices] ADD [DeviceTypeId] int NOT NULL DEFAULT 0;

ALTER TABLE [Devices] ADD [Serial] nvarchar(max) NOT NULL DEFAULT N'';

CREATE TABLE [DeviceTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DeviceTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [Producents] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Producents] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250508063312_UpdateDataStructureV4', N'9.0.4');

EXEC sp_rename N'[Programs].[ProducentId]', N'ProducerId', 'COLUMN';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250508070725_UpdateDataStructureV5', N'9.0.4');

ALTER TABLE [Devices] ADD [StatusId] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250508100813_UpdateDeviceModel', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250508101042_UpdateDeviceModelV2', N'9.0.4');

ALTER TABLE [Devices] ADD [StatusUpdate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250508101133_UpdateDeviceModelV3', N'9.0.4');

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'IsAdmin');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Users] ALTER COLUMN [IsAdmin] int NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250514082044_updateUserModelV6', N'9.0.4');

CREATE TABLE [ActionHistories] (
    [Id] int NOT NULL IDENTITY,
    [UserPermissionId] int NOT NULL,
    [DeviceId] int NULL,
    [ApplicationId] int NULL,
    [UserId] int NULL,
    [Date] datetime2 NOT NULL,
    [ActionId] int NOT NULL,
    CONSTRAINT [PK_ActionHistories] PRIMARY KEY ([Id])
);

CREATE TABLE [Actions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Actions] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520062348_addhistoryV2', N'9.0.4');

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ActionHistories]') AND [c].[name] = N'UserPermissionId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [ActionHistories] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [ActionHistories] ALTER COLUMN [UserPermissionId] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520082041_addhistoryv4', N'9.0.4');

ALTER TABLE [ActionHistories] ADD [ActionHistoryId] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250521105216_repairHistoryModel', N'9.0.4');

CREATE TABLE [ApplicationDetails] (
    [Id] int NOT NULL IDENTITY,
    [ApplicationId] int NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [ExpireDate] datetime2 NOT NULL,
    CONSTRAINT [PK_ApplicationDetails] PRIMARY KEY ([Id])
);

CREATE TABLE [Logins] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(max) NOT NULL,
    [ProgramId] int NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_Logins] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250522084253_addNewTables', N'9.0.4');

EXEC sp_rename N'[Logins].[UserName]', N'Username', 'COLUMN';

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Logins]') AND [c].[name] = N'Username');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Logins] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Logins] ALTER COLUMN [Username] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250812085644_newNames', N'9.0.4');

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Programs]') AND [c].[name] = N'ProducerId');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Programs] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Programs] DROP COLUMN [ProducerId];

CREATE TABLE [PermissionGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [PermissionIds] nvarchar(max) NOT NULL,
    [AutoCreated] bit NOT NULL,
    [DepartmentId] int NOT NULL,
    CONSTRAINT [PK_PermissionGroups] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250812122154_newFields_1208', N'9.0.4');

COMMIT;
GO

