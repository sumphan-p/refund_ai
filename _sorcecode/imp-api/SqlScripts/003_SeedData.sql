USE [IMP_DB]
GO

-- =============================================
-- Seed: Roles
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [imp].[Roles] WHERE [RoleName] = 'Admin')
BEGIN
    INSERT INTO [imp].[Roles] ([RoleName], [Description])
    VALUES ('Admin', N'ผู้ดูแลระบบ — สิทธิ์เต็มทุกเมนู')
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Roles] WHERE [RoleName] = 'User')
BEGIN
    INSERT INTO [imp].[Roles] ([RoleName], [Description])
    VALUES ('User', N'ผู้ใช้งานทั่วไป')
END
GO

-- =============================================
-- Seed: Menu
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'DASHBOARD')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('DASHBOARD', N'แดชบอร์ด', 'LayoutDashboard', '/dashboard', 1)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FILE_DATA')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('FILE_DATA', N'แฟ้มข้อมูล', 'FolderArchive', '/file-data', 2)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'IMPORT')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('IMPORT', N'ข้อมูลนำเข้า', 'FileInput', '/import', 3)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'IMPORT_EXCEL')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'IMPORT'),
        'IMPORT_EXCEL', N'รับข้อมูลนำเข้าจาก Excel', 'FileSpreadsheet', '/import/excel', 1
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'IMPORT_MANAGE')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'IMPORT'),
        'IMPORT_MANAGE', N'บริหารจัดการข้อมูลนำเข้า', 'FolderOpen', '/import/manage', 2
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'EXPORT')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('EXPORT', N'ข้อมูลส่งออก', 'FileOutput', '/export', 4)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'EXPORT_EXCEL')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'EXPORT'),
        'EXPORT_EXCEL', N'รับข้อมูลส่งออกจาก Excel', 'FileSpreadsheet', '/export/excel', 1
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'EXPORT_MANAGE')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'EXPORT'),
        'EXPORT_MANAGE', N'บริหารจัดการข้อมูลส่งออก', 'FolderOpen', '/export/manage', 2
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'STOCKCARD')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('STOCKCARD', N'Stock Card วัตถุดิบ', 'ClipboardList', '/stockcard', 5)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('FORMULA', N'สูตรการผลิต', 'FlaskConical', '/formula', 6)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA_M29')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA'),
        'FORMULA_M29', N'สูตรการผลิต มาตรา 29', 'FlaskConical', '/formula/m29', 1
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA_BOI')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA'),
        'FORMULA_BOI', N'สูตรการผลิต BOI', 'FlaskRound', '/formula/boi', 2
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'REPORT')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('REPORT', N'รายงาน', 'BarChart3', '/report', 7)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN')
BEGIN
    INSERT INTO [imp].[Menu] ([MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES ('ADMIN', N'บริหารจัดการระบบ', 'Settings', '/admin', 99)
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN_USERS')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN'),
        'ADMIN_USERS', N'จัดการผู้ใช้งาน', 'Users', '/admin/users', 1
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN_ROLES')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN'),
        'ADMIN_ROLES', N'จัดการบทบาท', 'Shield', '/admin/roles', 2
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN_PERMISSIONS')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN'),
        'ADMIN_PERMISSIONS', N'จัดการสิทธิ์', 'Lock', '/admin/permissions', 3
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN_MENUS')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN'),
        'ADMIN_MENUS', N'จัดการเมนู', 'Menu', '/admin/menus', 4
    )
END

IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN_USER_PERMS')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'ADMIN'),
        'ADMIN_USER_PERMS', N'จัดการสิทธิ์ผู้ใช้', 'UserCog', '/admin/user-permissions', 5
    )
END
GO

-- =============================================
-- Seed: Admin user (admin / P@ssw0rd)
-- BCrypt hash for "P@ssw0rd" with workFactor=12
-- =============================================
DECLARE @AdminUserId UNIQUEIDENTIFIER = NEWID()
IF NOT EXISTS (SELECT 1 FROM [imp].[Users] WHERE [UserName] = 'admin')
BEGIN
    INSERT INTO [imp].[Users] ([Id], [UserName], [PasswordHash], [DisplayName], [Email])
    VALUES (
        @AdminUserId,
        'admin',
        '$2a$12$ua2jnrVkwaZCs1/4JlhTxOZ2oK0s7H9tfl.vGtGBZ0lw7JB6BTQxK',
        N'ผู้ดูแลระบบ',
        'admin@example.com'
    )

    -- Assign Admin role
    INSERT INTO [imp].[UserRoles] ([UserId], [RoleId])
    VALUES (@AdminUserId, (SELECT [Id] FROM [imp].[Roles] WHERE [RoleName] = 'Admin'))

    PRINT 'Admin user created. Username: admin / Password: P@ssw0rd'
END
GO

-- =============================================
-- Seed: Admin role permissions — full access to all menus
-- =============================================
DECLARE @AdminRoleId INT = (SELECT [Id] FROM [imp].[Roles] WHERE [RoleName] = 'Admin')

INSERT INTO [imp].[RoleMenuPermissions] ([RoleId], [MenuId], [Visible], [CanCreate], [CanEdit], [CanReadOnly], [CanDelete])
SELECT @AdminRoleId, m.[Id], 1, 1, 1, 1, 1
FROM [imp].[Menu] m
WHERE NOT EXISTS (
    SELECT 1 FROM [imp].[RoleMenuPermissions] rmp
    WHERE rmp.[RoleId] = @AdminRoleId AND rmp.[MenuId] = m.[Id]
)
GO

PRINT 'Seed data completed.'
GO
