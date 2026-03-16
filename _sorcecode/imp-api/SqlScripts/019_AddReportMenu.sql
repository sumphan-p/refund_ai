-- =============================================
-- 019: Add Report Menu under มาตรา 29
-- =============================================

DECLARE @s29Id INT = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29');

IF @s29Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_REPORT')
BEGIN
    INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive)
    VALUES (@s29Id, 'PRIVILEGE_S29_REPORT', N'รายงาน', 'FileText', '/privilege/section29/report', 3, 1);
    PRINT 'Inserted menu: PRIVILEGE_S29_REPORT';
END
GO

-- Grant Admin role full permissions
DECLARE @adminRoleId INT = (SELECT Id FROM imp.Roles WHERE RoleName = 'Admin');

IF @adminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_REPORT'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_REPORT'), 1, 1, 1, 1, 1);
    END
    PRINT 'Granted Admin permissions on PRIVILEGE_S29_REPORT';
END
GO

-- Grant UserMenuPermissions for users who already have user-level permissions
DECLARE @menuCodes TABLE (MenuCode NVARCHAR(50), CanCreate BIT, CanEdit BIT, CanReadOnly BIT, CanDelete BIT);
INSERT INTO @menuCodes VALUES ('PRIVILEGE_S29_REPORT', 0, 0, 1, 0);

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

PRINT 'Granted UserMenuPermissions for PRIVILEGE_S29_REPORT';
GO
