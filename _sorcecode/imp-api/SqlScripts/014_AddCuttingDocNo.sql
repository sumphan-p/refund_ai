-- 014: Add BatchDocNo to stock_m29_batch
-- Format: "001/69" where 001 = running number, 69 = last 2 digits of Buddhist year

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('imp.stock_m29_batch') AND name = 'BatchDocNo')
BEGIN
    ALTER TABLE [imp].[stock_m29_batch]
        ADD [BatchDocNo] NVARCHAR(20) NULL;

    CREATE INDEX [IX_stock_m29_batch_batchdocno] ON [imp].[stock_m29_batch] ([BatchDocNo]);

    PRINT 'Added BatchDocNo column to imp.stock_m29_batch';
END
GO
