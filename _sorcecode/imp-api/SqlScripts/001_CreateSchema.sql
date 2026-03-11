-- =============================================
-- Create Schema: imp
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'imp')
BEGIN
    EXEC('CREATE SCHEMA imp')
END
GO

PRINT 'Schema [imp] created or already exists.'
GO
