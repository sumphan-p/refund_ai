-- 009_MigrateDataFromDbo.sql
-- อ่านข้อมูลจาก dbo.BOM_M29_HD, dbo.BOM_M29_DT, dbo.BOM_BOI_HD, dbo.BOM_BOI_DT
-- แล้ว insert เข้า imp.bom_m29_hd, imp.bom_m29_dt, imp.bom_boi_hd, imp.bom_boi_dt
-- รันซ้ำได้ (idempotent) — ลบข้อมูลเก่าใน imp ก่อน insert ใหม่

USE [IMP_DB]
GO

-- =============================================
-- Step 1: ดูโครงสร้าง column ของ dbo tables (debug)
-- =============================================
PRINT '=== dbo.BOM_M29_HD columns ==='
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BOM_M29_HD'
ORDER BY ORDINAL_POSITION

PRINT '=== dbo.BOM_M29_DT columns ==='
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BOM_M29_DT'
ORDER BY ORDINAL_POSITION

PRINT '=== dbo.BOM_BOI_HD columns ==='
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BOM_BOI_HD'
ORDER BY ORDINAL_POSITION

PRINT '=== dbo.BOM_BOI_DT columns ==='
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'BOM_BOI_DT'
ORDER BY ORDINAL_POSITION
GO

-- =============================================
-- Step 2: Migrate BOM_M29_HD → imp.bom_m29_hd
-- =============================================
PRINT ''
PRINT '--- Step 2: BOM_M29_HD → imp.bom_m29_hd ---'

-- ลบข้อมูลเก่า (DT จะถูกลบด้วย CASCADE)
DELETE FROM [imp].[bom_m29_hd];

-- ดึง matching columns (ยกเว้น Id)
DECLARE @m29hd_cols NVARCHAR(MAX) = '';
SELECT @m29hd_cols = @m29hd_cols +
    CASE WHEN @m29hd_cols = '' THEN '' ELSE ', ' END +
    QUOTENAME(src.COLUMN_NAME)
FROM INFORMATION_SCHEMA.COLUMNS src
INNER JOIN INFORMATION_SCHEMA.COLUMNS tgt
    ON src.COLUMN_NAME = tgt.COLUMN_NAME
WHERE src.TABLE_SCHEMA = 'dbo' AND src.TABLE_NAME = 'BOM_M29_HD'
  AND tgt.TABLE_SCHEMA = 'imp' AND tgt.TABLE_NAME = 'bom_m29_hd'
  AND src.COLUMN_NAME != 'Id'
ORDER BY tgt.ORDINAL_POSITION;

PRINT 'Matching columns: ' + @m29hd_cols;

IF @m29hd_cols != ''
BEGIN
    DECLARE @sql1 NVARCHAR(MAX) =
        'INSERT INTO [imp].[bom_m29_hd] (' + @m29hd_cols + ') ' +
        'SELECT ' + @m29hd_cols + ' FROM [dbo].[BOM_M29_HD]';
    PRINT @sql1;
    EXEC sp_executesql @sql1;
    PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows into imp.bom_m29_hd';
END
GO

-- =============================================
-- Step 3: Migrate BOM_M29_DT → imp.bom_m29_dt
-- =============================================
PRINT ''
PRINT '--- Step 3: BOM_M29_DT → imp.bom_m29_dt ---'

-- หา FK column ใน dbo.BOM_M29_DT
DECLARE @m29dt_fk NVARCHAR(128) = NULL;
IF COL_LENGTH('dbo.BOM_M29_DT', 'BomM29HdId') IS NOT NULL
    SET @m29dt_fk = 'BomM29HdId';
ELSE IF COL_LENGTH('dbo.BOM_M29_DT', 'ProductionFormulaNo') IS NOT NULL
    SET @m29dt_fk = 'ProductionFormulaNo';

PRINT 'FK column: ' + ISNULL(@m29dt_fk, '(not found)');

-- ดึง matching columns (ยกเว้น Id, BomM29HdId, ProductionFormulaNo)
-- ProductionFormulaNo ต้องยกเว้นเพราะ imp.bom_m29_dt ไม่มี column นี้ (ใช้ FK แทน)
DECLARE @m29dt_src NVARCHAR(MAX) = '';
DECLARE @m29dt_tgt NVARCHAR(MAX) = '';

SELECT @m29dt_src = @m29dt_src +
    CASE WHEN @m29dt_src = '' THEN '' ELSE ', ' END +
    'src.' + QUOTENAME(src.COLUMN_NAME),
   @m29dt_tgt = @m29dt_tgt +
    CASE WHEN @m29dt_tgt = '' THEN '' ELSE ', ' END +
    QUOTENAME(tgt.COLUMN_NAME)
FROM INFORMATION_SCHEMA.COLUMNS src
INNER JOIN INFORMATION_SCHEMA.COLUMNS tgt
    ON src.COLUMN_NAME = tgt.COLUMN_NAME
WHERE src.TABLE_SCHEMA = 'dbo' AND src.TABLE_NAME = 'BOM_M29_DT'
  AND tgt.TABLE_SCHEMA = 'imp' AND tgt.TABLE_NAME = 'bom_m29_dt'
  AND src.COLUMN_NAME NOT IN ('Id', 'BomM29HdId')
ORDER BY tgt.ORDINAL_POSITION;

PRINT 'Matching columns: ' + @m29dt_tgt;

IF @m29dt_fk IS NOT NULL AND @m29dt_src != ''
BEGIN
    DECLARE @sql2 NVARCHAR(MAX);

    IF @m29dt_fk = 'ProductionFormulaNo'
    BEGIN
        -- DT มี ProductionFormulaNo → join ตรงไป imp.bom_m29_hd
        SET @sql2 =
            'INSERT INTO [imp].[bom_m29_dt] ([BomM29HdId], ' + @m29dt_tgt + ') ' +
            'SELECT hd.[Id], ' + @m29dt_src + ' ' +
            'FROM [dbo].[BOM_M29_DT] src ' +
            'INNER JOIN [imp].[bom_m29_hd] hd ON src.[ProductionFormulaNo] = hd.[ProductionFormulaNo]';
    END
    ELSE
    BEGIN
        -- DT มี BomM29HdId → join ผ่าน dbo HD → imp HD
        SET @sql2 =
            'INSERT INTO [imp].[bom_m29_dt] ([BomM29HdId], ' + @m29dt_tgt + ') ' +
            'SELECT hd_new.[Id], ' + @m29dt_src + ' ' +
            'FROM [dbo].[BOM_M29_DT] src ' +
            'INNER JOIN [dbo].[BOM_M29_HD] hd_old ON src.[BomM29HdId] = hd_old.[Id] ' +
            'INNER JOIN [imp].[bom_m29_hd] hd_new ON hd_old.[ProductionFormulaNo] = hd_new.[ProductionFormulaNo]';
    END

    PRINT @sql2;
    EXEC sp_executesql @sql2;
    PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows into imp.bom_m29_dt';
END
GO

-- =============================================
-- Step 4: Migrate BOM_BOI_HD → imp.bom_boi_hd
-- =============================================
PRINT ''
PRINT '--- Step 4: BOM_BOI_HD → imp.bom_boi_hd ---'

DELETE FROM [imp].[bom_boi_hd];

DECLARE @boihd_cols NVARCHAR(MAX) = '';
SELECT @boihd_cols = @boihd_cols +
    CASE WHEN @boihd_cols = '' THEN '' ELSE ', ' END +
    QUOTENAME(src.COLUMN_NAME)
FROM INFORMATION_SCHEMA.COLUMNS src
INNER JOIN INFORMATION_SCHEMA.COLUMNS tgt
    ON src.COLUMN_NAME = tgt.COLUMN_NAME
WHERE src.TABLE_SCHEMA = 'dbo' AND src.TABLE_NAME = 'BOM_BOI_HD'
  AND tgt.TABLE_SCHEMA = 'imp' AND tgt.TABLE_NAME = 'bom_boi_hd'
  AND src.COLUMN_NAME != 'Id'
ORDER BY tgt.ORDINAL_POSITION;

PRINT 'Matching columns: ' + @boihd_cols;

IF @boihd_cols != ''
BEGIN
    DECLARE @sql3 NVARCHAR(MAX) =
        'INSERT INTO [imp].[bom_boi_hd] (' + @boihd_cols + ') ' +
        'SELECT ' + @boihd_cols + ' FROM [dbo].[BOM_BOI_HD]';
    PRINT @sql3;
    EXEC sp_executesql @sql3;
    PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows into imp.bom_boi_hd';
END
GO

-- =============================================
-- Step 5: Migrate BOM_BOI_DT → imp.bom_boi_dt
-- =============================================
PRINT ''
PRINT '--- Step 5: BOM_BOI_DT → imp.bom_boi_dt ---'

DECLARE @boidt_fk NVARCHAR(128) = NULL;
IF COL_LENGTH('dbo.BOM_BOI_DT', 'BomBoiHdId') IS NOT NULL
    SET @boidt_fk = 'BomBoiHdId';
ELSE IF COL_LENGTH('dbo.BOM_BOI_DT', 'ProductionFormulaNo') IS NOT NULL
    SET @boidt_fk = 'ProductionFormulaNo';

PRINT 'FK column: ' + ISNULL(@boidt_fk, '(not found)');

DECLARE @boidt_src NVARCHAR(MAX) = '';
DECLARE @boidt_tgt NVARCHAR(MAX) = '';

SELECT @boidt_src = @boidt_src +
    CASE WHEN @boidt_src = '' THEN '' ELSE ', ' END +
    'src.' + QUOTENAME(src.COLUMN_NAME),
   @boidt_tgt = @boidt_tgt +
    CASE WHEN @boidt_tgt = '' THEN '' ELSE ', ' END +
    QUOTENAME(tgt.COLUMN_NAME)
FROM INFORMATION_SCHEMA.COLUMNS src
INNER JOIN INFORMATION_SCHEMA.COLUMNS tgt
    ON src.COLUMN_NAME = tgt.COLUMN_NAME
WHERE src.TABLE_SCHEMA = 'dbo' AND src.TABLE_NAME = 'BOM_BOI_DT'
  AND tgt.TABLE_SCHEMA = 'imp' AND tgt.TABLE_NAME = 'bom_boi_dt'
  AND src.COLUMN_NAME NOT IN ('Id', 'BomBoiHdId')
ORDER BY tgt.ORDINAL_POSITION;

PRINT 'Matching columns: ' + @boidt_tgt;

IF @boidt_fk IS NOT NULL AND @boidt_src != ''
BEGIN
    DECLARE @sql4 NVARCHAR(MAX);

    IF @boidt_fk = 'ProductionFormulaNo'
    BEGIN
        SET @sql4 =
            'INSERT INTO [imp].[bom_boi_dt] ([BomBoiHdId], ' + @boidt_tgt + ') ' +
            'SELECT hd.[Id], ' + @boidt_src + ' ' +
            'FROM [dbo].[BOM_BOI_DT] src ' +
            'INNER JOIN [imp].[bom_boi_hd] hd ON src.[ProductionFormulaNo] = hd.[ProductionFormulaNo]';
    END
    ELSE
    BEGIN
        SET @sql4 =
            'INSERT INTO [imp].[bom_boi_dt] ([BomBoiHdId], ' + @boidt_tgt + ') ' +
            'SELECT hd_new.[Id], ' + @boidt_src + ' ' +
            'FROM [dbo].[BOM_BOI_DT] src ' +
            'INNER JOIN [dbo].[BOM_BOI_HD] hd_old ON src.[BomBoiHdId] = hd_old.[Id] ' +
            'INNER JOIN [imp].[bom_boi_hd] hd_new ON hd_old.[ProductionFormulaNo] = hd_new.[ProductionFormulaNo]';
    END

    PRINT @sql4;
    EXEC sp_executesql @sql4;
    PRINT 'Inserted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows into imp.bom_boi_dt';
END
GO

-- =============================================
-- Step 6: สรุปจำนวนข้อมูล
-- =============================================
PRINT ''
PRINT '========== Migration Summary =========='

SELECT 'dbo.BOM_M29_HD' AS [Table], COUNT(*) AS [Rows] FROM [dbo].[BOM_M29_HD]
UNION ALL
SELECT 'imp.bom_m29_hd', COUNT(*) FROM [imp].[bom_m29_hd]
UNION ALL
SELECT 'dbo.BOM_M29_DT', COUNT(*) FROM [dbo].[BOM_M29_DT]
UNION ALL
SELECT 'imp.bom_m29_dt', COUNT(*) FROM [imp].[bom_m29_dt]
UNION ALL
SELECT 'dbo.BOM_BOI_HD', COUNT(*) FROM [dbo].[BOM_BOI_HD]
UNION ALL
SELECT 'imp.bom_boi_hd', COUNT(*) FROM [imp].[bom_boi_hd]
UNION ALL
SELECT 'dbo.BOM_BOI_DT', COUNT(*) FROM [dbo].[BOM_BOI_DT]
UNION ALL
SELECT 'imp.bom_boi_dt', COUNT(*) FROM [imp].[bom_boi_dt]
ORDER BY [Table]

PRINT 'Migration complete!'
GO
