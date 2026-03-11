-- 007_CreateBomTables.sql
-- ตาราง BOM (Bill of Materials) สำหรับสูตรการผลิต
-- BOI = Board of Investment, M29 = มาตรา 29 (Section 19 ทวิ)
-- Idempotent: รันซ้ำได้ (DROP IF EXISTS → CREATE)

-- =============================================
-- Drop table เดิม (DT ก่อน HD เพราะมี FK)
-- =============================================
DROP TABLE IF EXISTS [imp].[bom_boi_dt];
DROP TABLE IF EXISTS [imp].[bom_boi_hd];
DROP TABLE IF EXISTS [imp].[bom_m29_dt];
DROP TABLE IF EXISTS [imp].[bom_m29_hd];
GO

-- =============================================
-- 1. imp.bom_boi_hd — สูตรการผลิต BOI (Header)
-- =============================================
CREATE TABLE [imp].[bom_boi_hd] (
    [Id]                    INT IDENTITY(1,1)   NOT NULL,
    [ProductionFormulaNo]   NVARCHAR(50)        NOT NULL,
    [DescriptionEn1]        NVARCHAR(500)       NULL,
    [DescriptionTh1]        NVARCHAR(500)       NULL,
    [ProductType]           NVARCHAR(100)       NULL,
    [CreatedBy]             NVARCHAR(100)       NULL,
    [CreatedDate]           DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
    [ModifiedBy]            NVARCHAR(100)       NULL,
    [ModifiedDate]          DATETIME2(7)        NULL,

    CONSTRAINT [PK_imp_bom_boi_hd] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_imp_bom_boi_hd_FormulaNo] UNIQUE NONCLUSTERED ([ProductionFormulaNo])
)
GO

PRINT 'Table [imp].[bom_boi_hd] created.'
GO

-- =============================================
-- 2. imp.bom_boi_dt — สูตรการผลิต BOI (Detail)
-- =============================================
CREATE TABLE [imp].[bom_boi_dt] (
    [Id]                    INT IDENTITY(1,1)   NOT NULL,
    [BomBoiHdId]            INT                 NOT NULL,
    [No]                    INT                 NOT NULL,
    [RawMaterialCode]       NVARCHAR(50)        NULL,
    [ProductType]           NVARCHAR(100)       NULL,
    [Unit]                  NVARCHAR(20)        NULL,
    [Ratio]                 DECIMAL(18,6)       NULL,
    [Scrap]                 DECIMAL(18,6)       NULL,
    [Remark]                NVARCHAR(500)       NULL,
    [CreatedBy]             NVARCHAR(100)       NULL,
    [CreatedDate]           DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_imp_bom_boi_dt] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_imp_bom_boi_dt_hd] FOREIGN KEY ([BomBoiHdId])
        REFERENCES [imp].[bom_boi_hd] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_imp_bom_boi_dt_HdNo] UNIQUE NONCLUSTERED ([BomBoiHdId], [No])
)

CREATE NONCLUSTERED INDEX [IX_imp_bom_boi_dt_BomBoiHdId]
ON [imp].[bom_boi_dt] ([BomBoiHdId])
GO

PRINT 'Table [imp].[bom_boi_dt] created.'
GO

-- =============================================
-- 3. imp.bom_m29_hd — สูตรการผลิต ม.29 (Header)
-- =============================================
CREATE TABLE [imp].[bom_m29_hd] (
    [Id]                    INT IDENTITY(1,1)   NOT NULL,
    [ProductionFormulaNo]   NVARCHAR(50)        NOT NULL,
    [DescriptionEn1]        NVARCHAR(500)       NULL,
    [DescriptionTh1]        NVARCHAR(500)       NULL,
    [ProductType]           NVARCHAR(100)       NULL,
    [CreatedBy]             NVARCHAR(100)       NULL,
    [CreatedDate]           DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),
    [ModifiedBy]            NVARCHAR(100)       NULL,
    [ModifiedDate]          DATETIME2(7)        NULL,

    CONSTRAINT [PK_imp_bom_m29_hd] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_imp_bom_m29_hd_FormulaNo] UNIQUE NONCLUSTERED ([ProductionFormulaNo])
)
GO

PRINT 'Table [imp].[bom_m29_hd] created.'
GO

-- =============================================
-- 4. imp.bom_m29_dt — สูตรการผลิต ม.29 (Detail)
-- =============================================
CREATE TABLE [imp].[bom_m29_dt] (
    [Id]                    INT IDENTITY(1,1)   NOT NULL,
    [BomM29HdId]            INT                 NOT NULL,
    [No]                    INT                 NOT NULL,
    [RawMaterialCode]       NVARCHAR(50)        NULL,
    [ProductType]           NVARCHAR(100)       NULL,
    [Unit]                  NVARCHAR(20)        NULL,
    [Ratio]                 DECIMAL(18,6)       NULL,
    [Scrap]                 DECIMAL(18,6)       NULL,
    [Remark]                NVARCHAR(500)       NULL,
    [CreatedBy]             NVARCHAR(100)       NULL,
    [CreatedDate]           DATETIME2(7)        NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT [PK_imp_bom_m29_dt] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_imp_bom_m29_dt_hd] FOREIGN KEY ([BomM29HdId])
        REFERENCES [imp].[bom_m29_hd] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_imp_bom_m29_dt_HdNo] UNIQUE NONCLUSTERED ([BomM29HdId], [No])
)

CREATE NONCLUSTERED INDEX [IX_imp_bom_m29_dt_BomM29HdId]
ON [imp].[bom_m29_dt] ([BomM29HdId])
GO

PRINT 'Table [imp].[bom_m29_dt] created.'
GO
