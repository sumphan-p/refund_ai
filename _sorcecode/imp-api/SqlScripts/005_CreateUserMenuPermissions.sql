USE [IMP_DB]
GO

-- =============================================
-- User-specific menu permissions (override/supplement role-based)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'imp' AND TABLE_NAME = 'UserMenuPermissions')
BEGIN
    CREATE TABLE [imp].[UserMenuPermissions] (
        [Id]            INT IDENTITY(1,1)   NOT NULL,
        [UserId]        UNIQUEIDENTIFIER    NOT NULL,
        [MenuId]        INT                 NOT NULL,
        [Visible]       BIT                 NOT NULL DEFAULT 0,
        [CanCreate]     BIT                 NOT NULL DEFAULT 0,
        [CanEdit]       BIT                 NOT NULL DEFAULT 0,
        [CanReadOnly]   BIT                 NOT NULL DEFAULT 0,
        [CanDelete]     BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT [PK_imp_UserMenuPermissions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_imp_UserMenuPermissions_Users] FOREIGN KEY ([UserId])
            REFERENCES [imp].[Users] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_imp_UserMenuPermissions_Menu] FOREIGN KEY ([MenuId])
            REFERENCES [imp].[Menu] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_imp_UserMenuPermissions_UserMenu] UNIQUE NONCLUSTERED ([UserId], [MenuId])
    )

    PRINT 'Table imp.UserMenuPermissions created.'
END
ELSE
    PRINT 'Table imp.UserMenuPermissions already exists.'
GO
