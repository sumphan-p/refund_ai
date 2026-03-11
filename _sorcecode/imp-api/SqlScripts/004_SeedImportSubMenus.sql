USE [IMP_DB]
GO

-- =============================================
-- Seed: Import sub-menus
-- =============================================
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
GO

-- =============================================
-- Grant Admin role full access to new sub-menus
-- =============================================
DECLARE @AdminRoleId INT = (SELECT [Id] FROM [imp].[Roles] WHERE [RoleName] = 'Admin')

INSERT INTO [imp].[RoleMenuPermissions] ([RoleId], [MenuId], [Visible], [CanCreate], [CanEdit], [CanReadOnly], [CanDelete])
SELECT @AdminRoleId, m.[Id], 1, 1, 1, 1, 1
FROM [imp].[Menu] m
WHERE m.[MenuCode] IN ('IMPORT_EXCEL', 'IMPORT_MANAGE')
AND NOT EXISTS (
    SELECT 1 FROM [imp].[RoleMenuPermissions] rmp
    WHERE rmp.[RoleId] = @AdminRoleId AND rmp.[MenuId] = m.[Id]
)
GO

PRINT 'Import sub-menus seeded.'
GO
