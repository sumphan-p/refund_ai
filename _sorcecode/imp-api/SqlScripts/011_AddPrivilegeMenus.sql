-- =============================================
-- 011: Add Privilege Menus (สิทธิประโยชน์)
-- =============================================

-- Parent: สิทธิประโยชน์
IF NOT EXISTS (SELECT 1 FROM imp.Menu WHERE MenuCode = 'PRIVILEGE')
BEGIN
    INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive)
    VALUES (NULL, 'PRIVILEGE', N'สิทธิประโยชน์', 'Award', NULL, 5, 1);
    PRINT 'Inserted menu: PRIVILEGE';
END
GO

-- Sub-parent: มาตรา 29
DECLARE @privilegeId INT = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE');

IF @privilegeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29')
BEGIN
    INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive)
    VALUES (@privilegeId, 'PRIVILEGE_S29', N'มาตรา 29', 'Scale', NULL, 1, 1);
    PRINT 'Inserted menu: PRIVILEGE_S29';
END
GO

-- Child: บริหารจัดการสิทธิ์ประโยชน์ มาตรา 29
DECLARE @s29Id INT = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29');

IF @s29Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_MANAGE')
BEGIN
    INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive)
    VALUES (@s29Id, 'PRIVILEGE_S29_MANAGE', N'บริหารจัดการสิทธิ์ประโยชน์ มาตรา 29', 'ClipboardList', '/privilege/section29/manage', 1, 1);
    PRINT 'Inserted menu: PRIVILEGE_S29_MANAGE';
END
GO

-- Grant Admin role full permissions on new menus
DECLARE @adminRoleId INT = (SELECT Id FROM imp.Roles WHERE RoleName = 'Admin');

IF @adminRoleId IS NOT NULL
BEGIN
    -- PRIVILEGE parent
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE'), 1, 0, 0, 0, 0);
    END

    -- PRIVILEGE_S29 sub-parent
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29'), 1, 0, 0, 0, 0);
    END

    -- PRIVILEGE_S29_MANAGE child
    IF NOT EXISTS (SELECT 1 FROM imp.RoleMenuPermissions WHERE RoleId = @adminRoleId AND MenuId = (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_MANAGE'))
    BEGIN
        INSERT INTO imp.RoleMenuPermissions (RoleId, MenuId, Visible, CanCreate, CanEdit, CanReadOnly, CanDelete)
        VALUES (@adminRoleId, (SELECT Id FROM imp.Menu WHERE MenuCode = 'PRIVILEGE_S29_MANAGE'), 1, 1, 1, 1, 1);
    END

    PRINT 'Granted Admin permissions on privilege menus';
END
GO
