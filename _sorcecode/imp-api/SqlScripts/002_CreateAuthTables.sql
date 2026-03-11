USE [IMP_DB]
GO

-- =============================================
-- 1. imp.Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'Users')
BEGIN
    CREATE TABLE [imp].[Users] (
        [Id]            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        [UserName]      NVARCHAR(256)       NOT NULL,
        [PasswordHash]  NVARCHAR(512)       NOT NULL,
        [DisplayName]   NVARCHAR(100)       NOT NULL,
        [Email]         NVARCHAR(256)       NULL,
        [IsActive]      BIT                 NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_Users] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_Users_UserName] UNIQUE NONCLUSTERED ([UserName])
    )

    CREATE NONCLUSTERED INDEX [IX_imp_Users_UserName]
    ON [imp].[Users] ([UserName])
    INCLUDE ([PasswordHash], [DisplayName], [Email], [IsActive])

    PRINT 'Table [imp].[Users] created.'
END
GO

-- =============================================
-- 2. imp.Roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'Roles')
BEGIN
    CREATE TABLE [imp].[Roles] (
        [Id]            INT IDENTITY(1,1)   NOT NULL,
        [RoleName]      NVARCHAR(100)       NOT NULL,
        [Description]   NVARCHAR(500)       NULL,
        [IsActive]      BIT                 NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_Roles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_Roles_RoleName] UNIQUE NONCLUSTERED ([RoleName])
    )

    PRINT 'Table [imp].[Roles] created.'
END
GO

-- =============================================
-- 3. imp.UserRoles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'UserRoles')
BEGIN
    CREATE TABLE [imp].[UserRoles] (
        [UserId]    UNIQUEIDENTIFIER    NOT NULL,
        [RoleId]    INT                 NOT NULL,

        CONSTRAINT [PK_imp_UserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId]),
        CONSTRAINT [FK_imp_UserRoles_Users] FOREIGN KEY ([UserId])
            REFERENCES [imp].[Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_imp_UserRoles_Roles] FOREIGN KEY ([RoleId])
            REFERENCES [imp].[Roles] ([Id]) ON DELETE CASCADE
    )

    PRINT 'Table [imp].[UserRoles] created.'
END
GO

-- =============================================
-- 4. imp.Menu
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'Menu')
BEGIN
    CREATE TABLE [imp].[Menu] (
        [Id]            INT IDENTITY(1,1)   NOT NULL,
        [ParentId]      INT                 NULL,
        [MenuCode]      NVARCHAR(50)        NOT NULL,
        [MenuName]      NVARCHAR(200)       NOT NULL,
        [Icon]          NVARCHAR(100)       NULL,
        [Route]         NVARCHAR(500)       NULL,
        [SortOrder]     INT                 NOT NULL DEFAULT 0,
        [IsActive]      BIT                 NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_Menu] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_Menu_MenuCode] UNIQUE NONCLUSTERED ([MenuCode]),
        CONSTRAINT [FK_imp_Menu_Parent] FOREIGN KEY ([ParentId])
            REFERENCES [imp].[Menu] ([Id])
    )

    PRINT 'Table [imp].[Menu] created.'
END
GO

-- =============================================
-- 5. imp.RoleMenuPermissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'RoleMenuPermissions')
BEGIN
    CREATE TABLE [imp].[RoleMenuPermissions] (
        [Id]            INT IDENTITY(1,1)   NOT NULL,
        [RoleId]        INT                 NOT NULL,
        [MenuId]        INT                 NOT NULL,
        [Visible]       BIT                 NOT NULL DEFAULT 0,
        [CanCreate]     BIT                 NOT NULL DEFAULT 0,
        [CanEdit]       BIT                 NOT NULL DEFAULT 0,
        [CanReadOnly]   BIT                 NOT NULL DEFAULT 0,
        [CanDelete]     BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT [PK_imp_RoleMenuPermissions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_imp_RoleMenuPermissions_Roles] FOREIGN KEY ([RoleId])
            REFERENCES [imp].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_imp_RoleMenuPermissions_Menu] FOREIGN KEY ([MenuId])
            REFERENCES [imp].[Menu] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_imp_RoleMenuPermissions_RoleMenu] UNIQUE NONCLUSTERED ([RoleId], [MenuId])
    )

    PRINT 'Table [imp].[RoleMenuPermissions] created.'
END
GO

-- =============================================
-- 6. imp.RefreshTokens
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'RefreshTokens')
BEGIN
    CREATE TABLE [imp].[RefreshTokens] (
        [Id]            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        [UserId]        UNIQUEIDENTIFIER    NOT NULL,
        [Token]         NVARCHAR(512)       NOT NULL,
        [ExpiresAt]     DATETIME2(7)        NOT NULL,
        [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
        [RevokedAt]     DATETIME2(7)        NULL,

        CONSTRAINT [PK_imp_RefreshTokens] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_imp_RefreshTokens_Users] FOREIGN KEY ([UserId])
            REFERENCES [imp].[Users] ([Id]) ON DELETE CASCADE
    )

    CREATE NONCLUSTERED INDEX [IX_imp_RefreshTokens_Token]
    ON [imp].[RefreshTokens] ([Token])
    INCLUDE ([UserId], [ExpiresAt], [RevokedAt])

    CREATE NONCLUSTERED INDEX [IX_imp_RefreshTokens_ExpiresAt]
    ON [imp].[RefreshTokens] ([ExpiresAt])
    WHERE [RevokedAt] IS NULL

    PRINT 'Table [imp].[RefreshTokens] created.'
END
GO

-- =============================================
-- 7. imp.PasswordResetTokens
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'PasswordResetTokens')
BEGIN
    CREATE TABLE [imp].[PasswordResetTokens] (
        [Id]            UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        [UserId]        UNIQUEIDENTIFIER    NOT NULL,
        [Token]         NVARCHAR(512)       NOT NULL,
        [ExpiresAt]     DATETIME2(7)        NOT NULL,
        [CreatedAt]     DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UsedAt]        DATETIME2(7)        NULL,

        CONSTRAINT [PK_imp_PasswordResetTokens] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_imp_PasswordResetTokens_Users] FOREIGN KEY ([UserId])
            REFERENCES [imp].[Users] ([Id]) ON DELETE CASCADE
    )

    CREATE NONCLUSTERED INDEX [IX_imp_PasswordResetTokens_Token]
    ON [imp].[PasswordResetTokens] ([Token])
    INCLUDE ([UserId], [ExpiresAt], [UsedAt])

    PRINT 'Table [imp].[PasswordResetTokens] created.'
END
GO
