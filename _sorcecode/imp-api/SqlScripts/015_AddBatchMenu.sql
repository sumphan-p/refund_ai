-- =============================================
-- 015: Add Batch Management Menu (บริหารจัดการ Batch มาตรา 29)
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

-- Grant Admin role full permissions on new menu
DECLARE @adminRoleId INT = (SELECT Id FROM imp.Roles WHERE RoleName = 'Admin');

IF @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_BATCH'), 1, 1, 1, 1, 1);
    END

    PRINT 'Granted Admin permissions on PRIVILEGE_S29_BATCH (RoleMenuPermissions)';
END
GO

-- Grant UserMenuPermissions for users who already have user-level permissions
DECLARE @menuCodes TABLE (MenuCode NVARCHAR(50), CanCreate BIT, CanEdit BIT, CanReadOnly BIT, CanDelete BIT);
INSERT INTO @menuCodes VALUES ('PRIVILEGE_S29_BATCH', 1, 1, 1, 1);

DECLARE @usersWithPerms TABLE (UserId UNIQUEIDENTIFIER);
INSERT INTO @usersWithPerms SELECT DISTINCT UserId FROM imp.UserMenuPermissions;

INSERT INTO imp.UserMenuPermissions (UserId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
SELECT u.UserId, m.Id, 1, mc.CanCreate, mc.CanEdit, mc.CanReadOnly, mc.CanDelete
FROM @usersWithPerms u
CROSS JOIN imp.Menu m
JOIN @menuCodes mc ON mc.MenuCode = m.MenuCode
WHERE NOT EXISTS (
    SELECT 1 FROM imp.UserMenuPermissions ump
    WHERE ump.UserId = u.UserId AND ump.MenuId = m.Id
);

PRINT 'Granted UserMenuPermissions for PRIVILEGE_S29_BATCH';
GO
