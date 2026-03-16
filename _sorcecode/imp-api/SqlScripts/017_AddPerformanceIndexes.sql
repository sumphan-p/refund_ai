-- =============================================
-- 017: Performance indexes for Section 19 bis queries
-- =============================================

-- export_excel: WHERE clause (Section19Bis, CurrentStatus) + ORDER BY (LoadingDate)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_export_excel_s19bis_status' AND object_id = OBJECT_ID('imp.export_excel'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_export_excel_s19bis_status
    ON imp.export_excel (Section19Bis, CurrentStatus)
    INCLUDE (Section19BisNo, LoadingDate, DeclarNo, ItemDeclarNo,
             ProductCode, DescriptionTh1, DescriptionEn1,
             QtyDeclar, QtyDeclarUnit, NetWeight, FOBTHB,
             InvoiceNo, BuyerName, ImportTaxIncentiveId, ImportDeclarNo);
END
GO

-- export_excel: ORDER BY LoadingDate DESC
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_export_excel_loadingdate' AND object_id = OBJECT_ID('imp.export_excel'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_export_excel_loadingdate
    ON imp.export_excel (LoadingDate DESC, DeclarNo, ItemDeclarNo);
END
GO

-- m29_batch_item: LEFT JOIN on ExportExcelId (ตรวจสอบว่าจัดชุดแล้วหรือยัง)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_m29_batch_item_excelid_header' AND object_id = OBJECT_ID('imp.m29_batch_item'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_m29_batch_item_excelid_header
    ON imp.m29_batch_item (ExportExcelId, BatchHeaderId);
END
GO
