-- =============================================
-- 001-batch-add-menu: Add Batch Management Menu
-- เมนู "บริหารจัดการ มาตรา 29 ( Batch )" ใต้ PRIVILEGE_S29
-- =============================================

-- Child: บริหารจัดการ มาตรา 29 ( Batch )
DECLARE @s29Id INT = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29');

IF @s29Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH')
BEGIN
    INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive)
    VALUES (@s29Id, 'PRIVILEGE_S29_BATCH', N'บริหารจัดการ มาตรา 29 ( Batch )', 'Layers', '/privilege/section29/manage_batch', 2, 1);
    PRINT 'Inserted menu: PRIVILEGE_S29_BATCH';
END
GO

-- =============================================
-- Grant Admin role full permissions on new menu
-- =============================================
DECLARE @adminRoleId INT = (SELECT Id FROM imp.Roles WHERE RoleName = 'Admin');

IF @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH'), 1, 1, 1, 1, 1);
    END

    PRINT 'Granted Admin permissions on PRIVILEGE_S29_BATCH';
END
GO

-- =============================================
-- Grant UserMenuPermissions for users who already have user-level permissions
-- =============================================
DECLARE @batchMenuId INT = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH');

IF @batchMenuId IS NOT NULL
BEGIN
    INSERT INTO imp.UserMenuPermissions (UserId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
    SELECT DISTINCT ump.UserId, @batchMenuId, 1, 1, 1, 1, 1
    FROM imp.UserMenuPermissions ump
    WHERE NOT EXISTS (
        SELECT 1 FROM imp.UserMenuPermissions existing
        WHERE existing.UserId = ump.UserId AND existing.MenuId = @batchMenuId
    );

    PRINT 'Granted UserMenuPermissions for PRIVILEGE_S29_BATCH';
END
GO
