-- 005_CreateImportExcelTable.sql
-- ตาราง import_excel สำหรับเก็บข้อมูลนำเข้าจาก Excel (ใบขนสินค้าขาเข้า)
-- Unique key: DeclarNo + ItemDeclarNo (MERGE upsert)

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'import_excel' AND schema_id = SCHEMA_ID('imp'))
BEGIN
    CREATE TABLE imp.import_excel (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,

        -- Key fields (unique constraint)
        DeclarNo                NVARCHAR(50)    NOT NULL,   -- เลขที่ใบขนสินค้า
        ItemDeclarNo            INT             NOT NULL,   -- ลำดับรายการในใบขน

        -- Company info
        CustomerName            NVARCHAR(200)   NULL,
        CompanyTaxNo            NVARCHAR(20)    NULL,
        RefNo                   NVARCHAR(50)    NULL,

        -- Shipment info
        JobNo                   NVARCHAR(50)    NULL,
        VesselName              NVARCHAR(100)   NULL,
        Voy                     NVARCHAR(50)    NULL,
        ExportIncentiveId       NVARCHAR(50)    NULL,
        TradPartner             NVARCHAR(50)    NULL,
        TransportMode           NVARCHAR(10)    NULL,
        ReloadPort              NVARCHAR(10)    NULL,
        Subgate                 NVARCHAR(10)    NULL,
        InspectionCode          NVARCHAR(50)    NULL,       -- รหัสตรวจปล่อยนอกสถานที่
        ApprovedPort            NVARCHAR(10)    NULL,
        ETA                     NVARCHAR(20)    NULL,
        MasterBL                NVARCHAR(50)    NULL,
        HouseBL                 NVARCHAR(50)    NULL,

        -- Factory
        FactoryNo               NVARCHAR(50)    NULL,
        EstablishNo             NVARCHAR(50)    NULL,
        ApprovedNo              NVARCHAR(50)    NULL,

        -- Buyer
        StatusBuyer             NVARCHAR(10)    NULL,
        LevelBuyer              NVARCHAR(10)    NULL,
        ExporterRef             NVARCHAR(50)    NULL,
        PurchaseNo              NVARCHAR(50)    NULL,
        OtherRefNo              NVARCHAR(50)    NULL,

        -- Shipper / Country
        ShipperName             NVARCHAR(200)   NULL,
        PurchaseCountry         NVARCHAR(10)    NULL,
        OriginCountry           NVARCHAR(10)    NULL,
        CountryOfLoading        NVARCHAR(10)    NULL,

        -- Payment
        PaymentTerm             NVARCHAR(10)    NULL,
        IncoTerm                NVARCHAR(10)    NULL,
        Currency                NVARCHAR(10)    NULL,
        ExchRate                DECIMAL(18,4)   NULL,

        -- Charges
        InLandCharge            DECIMAL(18,4)   NULL,
        InLandChargeCurrency    NVARCHAR(10)    NULL,
        InLandChargeTHB         DECIMAL(18,4)   NULL,
        Freight                 DECIMAL(18,4)   NULL,
        FreightCurrency         NVARCHAR(10)    NULL,
        FreightTHB              DECIMAL(18,4)   NULL,
        Insurance               DECIMAL(18,4)   NULL,
        InsuranceCurrency       NVARCHAR(10)    NULL,
        InsuranceTHB            DECIMAL(18,4)   NULL,
        Packing                 DECIMAL(18,4)   NULL,
        PackingCurrency         NVARCHAR(10)    NULL,
        PackingTHB              DECIMAL(18,4)   NULL,
        ForeInLand              DECIMAL(18,4)   NULL,
        ForeInLandCurrency      NVARCHAR(10)    NULL,
        ForeInLandTHB           DECIMAL(18,4)   NULL,
        Landing                 DECIMAL(18,4)   NULL,
        LandingCurrency         NVARCHAR(10)    NULL,
        LandingTHB              DECIMAL(18,4)   NULL,
        OtherCharge1            DECIMAL(18,4)   NULL,
        OtherCharge1Currency    NVARCHAR(10)    NULL,
        OtherCharge1THB         DECIMAL(18,4)   NULL,
        OtherCharge2            DECIMAL(18,4)   NULL,
        OtherCharge2Currency    NVARCHAR(10)    NULL,
        OtherCharge2THB         DECIMAL(18,4)   NULL,

        -- AEO / Invoice
        AEORefNo                NVARCHAR(50)    NULL,
        InvoiceNo               NVARCHAR(50)    NULL,
        InvDate                 NVARCHAR(20)    NULL,
        ItemNo                  DECIMAL(18,4)   NULL,

        -- Product
        ProductCode             NVARCHAR(50)    NULL,
        DescriptionEn1          NVARCHAR(500)   NULL,
        DescriptionEn2          NVARCHAR(500)   NULL,
        DescriptionTh1          NVARCHAR(500)   NULL,
        DescriptionTh2          NVARCHAR(500)   NULL,
        PermitNo                NVARCHAR(50)    NULL,
        ShippingMark            NVARCHAR(500)   NULL,
        Remark                  NVARCHAR(500)   NULL,
        RtcProductCode          NVARCHAR(50)    NULL,
        Brand                   NVARCHAR(100)   NULL,
        TarifClass              NVARCHAR(20)    NULL,
        StateCode               NVARCHAR(10)    NULL,
        StateUnit               NVARCHAR(10)    NULL,
        Sequence                NVARCHAR(20)    NULL,
        Privilege               NVARCHAR(10)    NULL,

        -- Privilege details
        ReasonReserveRight      NVARCHAR(200)   NULL,       -- สาเหตุขอสงวนสิทธิ
        TariffDispute           NVARCHAR(200)   NULL,       -- พิกัดศุลกากรโต้แย้ง
        SQDispute               NVARCHAR(50)    NULL,
        RateDispute             NVARCHAR(50)    NULL,
        PrivilegeDispute        NVARCHAR(50)    NULL,
        TypeOfTariff            NVARCHAR(50)    NULL,
        Origin                  NVARCHAR(10)    NULL,
        TypeOfProduct           NVARCHAR(50)    NULL,
        TypeOfProduct2          NVARCHAR(50)    NULL,
        CommNo                  NVARCHAR(50)    NULL,
        PaintCode               NVARCHAR(50)    NULL,
        UphCode                 NVARCHAR(50)    NULL,
        ProductYear             NVARCHAR(10)    NULL,

        -- Quantity / Weight
        NetWeight               DECIMAL(18,4)   NULL,
        QtyDegree               DECIMAL(18,4)   NULL,
        Quantity                DECIMAL(18,4)   NULL,
        QuantityUnit            NVARCHAR(10)    NULL,
        QtyTariff               DECIMAL(18,4)   NULL,
        QtyUnit                 NVARCHAR(10)    NULL,
        PackNo                  DECIMAL(18,4)   NULL,
        PackUnit                NVARCHAR(10)    NULL,

        -- Price / Value
        UnitPrice               DECIMAL(18,4)   NULL,
        InvoiceAmount           DECIMAL(18,4)   NULL,       -- ราคาสินค้าตาม invoice
        AmountCIF               DECIMAL(18,4)   NULL,
        CIFCurrency             NVARCHAR(10)    NULL,
        CIFTHB                  DECIMAL(18,4)   NULL,
        AddPrice                DECIMAL(18,4)   NULL,       -- เพิ่มราคา
        AddPriceTHB             DECIMAL(18,4)   NULL,
        CIFReductionRate        DECIMAL(18,4)   NULL,       -- อัตราลดหย่อน CIF
        AssessedPrice           DECIMAL(18,4)   NULL,       -- ราคาประเมินอากร
        AssessedAmount          DECIMAL(18,4)   NULL,       -- ตามประเมิน

        -- Excise / Tax
        ExciseTariff            NVARCHAR(50)    NULL,
        UsedQty                 DECIMAL(18,4)   NULL,       -- ปริมาณที่ใช้
        AssessedQty             DECIMAL(18,4)   NULL,       -- ปริมาณที่ประเมิน
        AssessedQtyUnit         NVARCHAR(10)    NULL,
        SubQty                  NVARCHAR(50)    NULL,
        OriginCriteria          NVARCHAR(50)    NULL,       -- เกณฑ์ถิ่นกำเนิด
        CerExporterTax          NVARCHAR(50)    NULL,
        ImportTaxInc            NVARCHAR(50)    NULL,
        AHTNCode                NVARCHAR(50)    NULL,

        -- Material / Rights
        MaterialCode            NVARCHAR(50)    NULL,       -- รหัสวัตถุดิบ
        DangerousCode           NVARCHAR(50)    NULL,       -- รหัสสินค้าอันตราย
        UsePrivilege            NVARCHAR(50)    NULL,       -- ใช้สิทธิ
        BOICardNo               NVARCHAR(100)   NULL,       -- เลขที่บัตร BOI
        ProductionFormula       NVARCHAR(100)   NULL,       -- เลขที่สูตรการผลิต
        IsFreebie               NVARCHAR(10)    NULL,       -- เป็นของแถม
        RefDeclarNo             NVARCHAR(50)    NULL,       -- อ้างอิงใบขนสินค้า
        RefItemNo               DECIMAL(18,4)   NULL,       -- รายการที่

        -- Duty / Tax rates
        DutyRate                DECIMAL(18,4)   NULL,
        DutyRateS               DECIMAL(18,4)   NULL,
        DutyTextTHB             DECIMAL(18,4)   NULL,
        ExciseTaxRate           NVARCHAR(50)    NULL,       -- อัตราภาษีสรรพสามิต
        ExciseTax               NVARCHAR(50)    NULL,
        InteriorTax             NVARCHAR(50)    NULL,       -- ภาษีเพื่อมหาดไทย
        OtherTax                NVARCHAR(50)    NULL,       -- ภาษีอื่น ๆ
        Fee                     NVARCHAR(50)    NULL,       -- ค่าธรรมเนียม
        VATBase                 DECIMAL(18,4)   NULL,       -- ฐานภาษีมูลค่าเพิ่ม
        Vat                     DECIMAL(18,4)   NULL,
        TotalDutyVAT            DECIMAL(18,4)   NULL,

        -- Status / Timestamps
        EDIDateTime             NVARCHAR(30)    NULL,
        StampDateTime           NVARCHAR(30)    NULL,
        EDIStatus               NVARCHAR(10)    NULL,
        RemarkInternal          NVARCHAR(500)   NULL,       -- หมายเหตุ(ไม่ส่งกรม)
        DepositAmt              DECIMAL(18,4)   NULL,

        -- Audit
        CreatedAt               DATETIME2(7)    NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt               DATETIME2(7)    NULL,
        CreatedBy               NVARCHAR(100)   NULL,
        UpdatedBy               NVARCHAR(100)   NULL,

        CONSTRAINT UQ_import_excel_declar UNIQUE (DeclarNo, ItemDeclarNo)
    );

    CREATE INDEX IX_import_excel_CustomerName ON imp.import_excel (CustomerName);
    CREATE INDEX IX_import_excel_CompanyTaxNo ON imp.import_excel (CompanyTaxNo);
    CREATE INDEX IX_import_excel_InvoiceNo ON imp.import_excel (InvoiceNo);
    CREATE INDEX IX_import_excel_ProductCode ON imp.import_excel (ProductCode);
    CREATE INDEX IX_import_excel_UsePrivilege ON imp.import_excel (UsePrivilege);

    PRINT 'Table imp.import_excel created successfully.';
END
ELSE
BEGIN
    PRINT 'Table imp.import_excel already exists.';
END
GO
