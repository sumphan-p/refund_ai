-- 008_UpdateFormulaMenu.sql
-- แก้ไข MenuCode และ Route ของเมนูสูตรการผลิต ม.29 ให้ตรงกับ frontend

-- อัพเดท FORMULA_29 → FORMULA_M29 และ route → /formula/m29
IF EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA_29')
BEGIN
    UPDATE [imp].[Menu]
    SET [MenuCode] = 'FORMULA_M29',
        [Route] = '/formula/m29'
    WHERE [MenuCode] = 'FORMULA_29';

    PRINT 'Updated FORMULA_29 -> FORMULA_M29, route -> /formula/m29'
END

-- กรณี FORMULA_M29 ยังไม่มี (ไม่เคยรัน seed)
IF NOT EXISTS (SELECT 1 FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA_M29')
BEGIN
    INSERT INTO [imp].[Menu] ([ParentId], [MenuCode], [MenuName], [Icon], [Route], [SortOrder])
    VALUES (
        (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA'),
        'FORMULA_M29', N'สูตรการผลิต มาตรา 29', 'FlaskConical', '/formula/m29', 1
    );

    PRINT 'Inserted FORMULA_M29 menu'
END

-- ให้ Admin role มีสิทธิ์เข้าถึงเมนูนี้
DECLARE @AdminRoleId INT = (SELECT [Id] FROM [imp].[Roles] WHERE [RoleName] = 'Admin')
DECLARE @MenuId INT = (SELECT [Id] FROM [imp].[Menu] WHERE [MenuCode] = 'FORMULA_M29')

IF @AdminRoleId IS NOT NULL AND @MenuId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [imp].[RoleMenuPermissions] WHERE [RoleId] = @AdminRoleId AND [MenuId] = @MenuId)
BEGIN
    INSERT INTO [imp].[RoleMenuPermissions] ([RoleId], [MenuId], [Visible], [CanCreate], [CanEdit], [CanReadOnly], [CanDelete])
    VALUES (@AdminRoleId, @MenuId, 1, 1, 1, 1, 1);

    PRINT 'Granted Admin full access to FORMULA_M29'
END
GO

PRINT 'Formula menu update complete.'
GO
