-- =============================================
-- 018: Drop stock_m29_on_hand table
-- No longer used — QtyOnHand now calculated from
-- SUM(QtyBalance) FROM stock_m29_lot WHERE Status = 'ACTIVE'
-- =============================================

IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_on_hand')
BEGIN
    DROP TABLE [imp].[stock_m29_on_hand];
    PRINT 'Dropped table imp.stock_m29_on_hand';
END
GO
