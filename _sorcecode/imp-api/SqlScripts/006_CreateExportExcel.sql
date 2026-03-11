-- 006_CreateExportExcel.sql
-- ตาราง export_excel สำหรับเก็บข้อมูลส่งออกจาก Excel (ใบขนสินค้าขาออก)
-- Unique key: DeclarNo + ItemDeclarNo (MERGE upsert)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'export_excel' AND schema_id = SCHEMA_ID('imp'))
BEGIN
    CREATE TABLE imp.export_excel (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,

        -- Key fields (unique constraint)
        DeclarNo                NVARCHAR(50)    NOT NULL,   -- เลขที่ใบขนสินค้าขาออก
        ItemDeclarNo            INT             NOT NULL,   -- ลำดับรายการในใบขน

        -- Exporter info
        ExporterName            NVARCHAR(200)   NULL,       -- ชื่อผู้ส่งออก
        TaxId                   NVARCHAR(20)    NULL,       -- เลขประจำตัวผู้เสียภาษี
        BranchSeq               NVARCHAR(10)    NULL,       -- ลำดับสาขา
        DocumentType            NVARCHAR(10)    NULL,       -- ประเภทเอกสาร
        BuyerName               NVARCHAR(200)   NULL,       -- ชื่อผู้ซื้อ

        -- Invoice
        InvoiceNo               NVARCHAR(50)    NULL,
        InvDate                 NVARCHAR(20)    NULL,
        InvoiceItemNo           INT             NULL,       -- ลำดับรายการ Invoice

        -- Dates / Status
        SubmissionDate          NVARCHAR(20)    NULL,       -- วันที่ยื่น
        ReleaseDate             NVARCHAR(20)    NULL,       -- วันที่ตรวจปล่อย
        LoadingDate             NVARCHAR(20)    NULL,       -- วันที่บรรทุก
        CurrentStatus           NVARCHAR(50)    NULL,       -- สถานะปัจจุบัน

        -- Product
        ProductCode             NVARCHAR(50)    NULL,       -- รหัสสินค้า
        Brand                   NVARCHAR(100)   NULL,
        PurchaseOrder           NVARCHAR(100)   NULL,
        DescriptionEn1          NVARCHAR(500)   NULL,
        DescriptionEn2          NVARCHAR(500)   NULL,
        DescriptionEn3          NVARCHAR(500)   NULL,
        DescriptionEn4          NVARCHAR(500)   NULL,
        DescriptionTh1          NVARCHAR(500)   NULL,
        DescriptionTh2          NVARCHAR(500)   NULL,
        DescriptionTh3          NVARCHAR(500)   NULL,
        DescriptionTh4          NVARCHAR(500)   NULL,

        -- Terms / Tariff
        TermOfPayment           NVARCHAR(50)    NULL,
        TariffCode              NVARCHAR(20)    NULL,       -- พิกัดศุลกากร
        TariffType              NVARCHAR(10)    NULL,
        StatisticalCode         NVARCHAR(20)    NULL,       -- รหัสสถิติ
        StatisticalUnit         NVARCHAR(10)    NULL,

        -- Quantity / Weight
        PackageCount            DECIMAL(18,4)   NULL,
        PackageUnit             NVARCHAR(10)    NULL,
        QtyInvoice              DECIMAL(18,4)   NULL,       -- ปริมาณ (Invoice)
        QtyInvoiceUnit          NVARCHAR(10)    NULL,
        QtyDeclar               DECIMAL(18,4)   NULL,       -- ปริมาณ (ใบขน)
        QtyDeclarUnit           NVARCHAR(10)    NULL,
        NetWeight               DECIMAL(18,4)   NULL,
        NetWeightUnit           NVARCHAR(10)    NULL,

        -- Price / Value
        UnitPrice               DECIMAL(18,4)   NULL,
        FOBForeign              DECIMAL(18,4)   NULL,       -- FOB (สกุลเงินต่างประเทศ)
        CurrencyCode            NVARCHAR(10)    NULL,
        FOBTHB                  DECIMAL(18,4)   NULL,       -- FOB (บาท)

        -- Shipment
        VesselName              NVARCHAR(100)   NULL,
        ExportPortCode          NVARCHAR(10)    NULL,       -- รหัสท่าส่งออก
        InspectionLocationCode  NVARCHAR(50)    NULL,       -- รหัสตรวจปล่อยนอกสถานที่
        BuyerCountryCode        NVARCHAR(10)    NULL,       -- รหัสประเทศผู้ซื้อ
        DestinationCountryCode  NVARCHAR(10)    NULL,       -- รหัสประเทศปลายทาง

        -- Privileges
        Compensation            NVARCHAR(10)    NULL,       -- ชดเชย
        CompensationNo          NVARCHAR(50)    NULL,
        BOI                     NVARCHAR(10)    NULL,
        BOINo                   NVARCHAR(100)   NULL,
        FormulaBOI              NVARCHAR(100)   NULL,       -- สูตร BOI
        Section19Bis            NVARCHAR(10)    NULL,       -- มาตรา 19 ทวิ
        Section19BisNo          NVARCHAR(50)    NULL,
        RightsTransferNo        NVARCHAR(50)    NULL,       -- เลขที่โอนสิทธิ
        Bond                    NVARCHAR(10)    NULL,       -- คลังสินค้าทัณฑ์บน

        -- Model
        ModelNo                 NVARCHAR(50)    NULL,
        ModelVer                NVARCHAR(20)    NULL,
        ModelCompTax            NVARCHAR(50)    NULL,       -- Model ชดเชยอากร

        -- Zone / Incentive
        EPZ                     NVARCHAR(10)    NULL,       -- เขตอุตสาหกรรมส่งออก
        FZ                      NVARCHAR(10)    NULL,       -- เขตปลอดอากร
        ImportTaxIncentiveId    NVARCHAR(50)    NULL,
        ExportTaxIncentiveId    NVARCHAR(50)    NULL,
        ReExport                NVARCHAR(10)    NULL,       -- ส่งกลับออกไป
        NetReturn               NVARCHAR(10)    NULL,       -- คืนอากรสุทธิ
        SpecialPrivilegeCode    NVARCHAR(20)    NULL,       -- รหัสสิทธิพิเศษ
        OriginCountryCode       NVARCHAR(10)    NULL,       -- รหัสประเทศแหล่งกำเนิด

        -- Import reference
        ImportDeclarNo          NVARCHAR(50)    NULL,       -- เลขที่ใบขนสินค้าขาเข้า
        ImportDeclarItemNo      DECIMAL(18,4)   NULL,       -- ลำดับรายการใบขนขาเข้า

        -- Short (ขาดจำนวน)
        ShortDeclar             NVARCHAR(50)    NULL,
        ShortPack               DECIMAL(18,4)   NULL,
        ShortQty                DECIMAL(18,4)   NULL,
        ShortNetWeight          DECIMAL(18,4)   NULL,
        ShortFOBForeign         DECIMAL(18,4)   NULL,
        ShortFOBTHB             DECIMAL(18,4)   NULL,

        -- Short Post (ขาดจำนวนหลังตรวจปล่อย)
        ShortPostDeclar         NVARCHAR(50)    NULL,
        ShortPostPack           DECIMAL(18,4)   NULL,
        ShortPostQty            DECIMAL(18,4)   NULL,
        ShortPostNetWeight      DECIMAL(18,4)   NULL,
        ShortPostFOBForeign     DECIMAL(18,4)   NULL,
        ShortPostFOBTHB         DECIMAL(18,4)   NULL,

        -- Permits / Reference
        PermitNo1               NVARCHAR(50)    NULL,
        PermitNo2               NVARCHAR(50)    NULL,
        PermitNo3               NVARCHAR(50)    NULL,
        BookingNo               NVARCHAR(50)    NULL,
        HouseBLNo               NVARCHAR(50)    NULL,
        Remark                  NVARCHAR(500)   NULL,

        -- Audit
        CreatedAt               DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2(7)    NULL,
        CreatedBy               NVARCHAR(100)   NULL,
        UpdatedBy               NVARCHAR(100)   NULL,

        CONSTRAINT UQ_export_excel_declar UNIQUE (DeclarNo, ItemDeclarNo)
    );

    CREATE INDEX IX_export_excel_ExporterName ON imp.export_excel (ExporterName);
    CREATE INDEX IX_export_excel_TaxId ON imp.export_excel (TaxId);
    CREATE INDEX IX_export_excel_InvoiceNo ON imp.export_excel (InvoiceNo);
    CREATE INDEX IX_export_excel_ProductCode ON imp.export_excel (ProductCode);
    CREATE INDEX IX_export_excel_BuyerName ON imp.export_excel (BuyerName);

    PRINT 'Table imp.export_excel created successfully.';
END
ELSE
BEGIN
    PRINT 'Table imp.export_excel already exists.';
END
GO
