-- =============================================
-- 013: Drop old stock_ tables and recreate as stock_m29_
-- =============================================

-- Drop old tables (order matters due to FK constraints)
IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_cutting')
    DROP TABLE [imp].[stock_cutting];

IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_card')
    DROP TABLE [imp].[stock_card];

IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_lot')
    DROP TABLE [imp].[stock_lot];

IF EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_on_hand')
    DROP TABLE [imp].[stock_on_hand];

PRINT 'Dropped old stock_ tables';
GO

-- 1. stock_m29_lot
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_lot')
BEGIN
    CREATE TABLE [imp].[stock_m29_lot] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [ImportDeclarNo]      NVARCHAR(50)      NOT NULL,
        [ImportItemNo]        INT               NOT NULL,
        [ImportDate]          DATE              NOT NULL,
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,

        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        [QtyOriginal]         DECIMAL(18,6)     NOT NULL,
        [QtyUsed]             DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL,
        [QtyTransferred]      DECIMAL(18,6)     NOT NULL DEFAULT 0,

        [UnitPrice]           DECIMAL(18,4)     NULL,
        [CIFValueTHB]         DECIMAL(18,4)     NULL,
        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyPerUnit]         DECIMAL(18,6)     NULL,
        [TotalDutyVAT]        DECIMAL(18,4)     NULL,

        [ImportTaxIncId]      NVARCHAR(50)      NULL,
        [BOICardNo]           NVARCHAR(100)     NULL,
        [ProductionFormulaNo] NVARCHAR(50)      NULL,

        [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'ACTIVE',
        [ExpiryDate]          DATE              NULL,

        [CreatedBy]           NVARCHAR(100)     NULL,
        [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_stock_m29_lot] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_stock_m29_lot] UNIQUE ([ImportDeclarNo], [ImportItemNo])
    );

    CREATE INDEX [IX_stock_m29_lot_fifo] ON [imp].[stock_m29_lot] ([RawMaterialCode], [PrivilegeType], [ImportDate], [Status]);
    CREATE INDEX [IX_stock_m29_lot_privilege] ON [imp].[stock_m29_lot] ([PrivilegeType], [Status]);
    CREATE INDEX [IX_stock_m29_lot_expiry] ON [imp].[stock_m29_lot] ([ExpiryDate], [Status]);

    PRINT 'Created table imp.stock_m29_lot';
END
GO

-- 2. stock_m29_card
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_card')
BEGIN
    CREATE TABLE [imp].[stock_m29_card] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [TransactionDate]     DATE              NOT NULL,
        [TransactionType]     NVARCHAR(20)      NOT NULL,
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,

        [ImportDeclarNo]      NVARCHAR(50)      NULL,
        [ImportItemNo]        INT               NULL,
        [ImportDate]          DATE              NULL,

        [ExportDeclarNo]      NVARCHAR(50)      NULL,
        [ExportItemNo]        INT               NULL,

        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        [QtyIn]               DECIMAL(18,6)     NULL,
        [QtyOut]              DECIMAL(18,6)     NULL,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL,

        [UnitPrice]           DECIMAL(18,4)     NULL,
        [CIFValueTHB]         DECIMAL(18,4)     NULL,
        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyAmount]          DECIMAL(18,4)     NULL,
        [VATAmount]           DECIMAL(18,4)     NULL,

        [ImportTaxIncId]      NVARCHAR(50)      NULL,
        [BOICardNo]           NVARCHAR(100)     NULL,
        [ProductionFormulaNo] NVARCHAR(50)      NULL,
        [CompensationNo]      NVARCHAR(50)      NULL,
        [TransferTableNo]     NVARCHAR(50)      NULL,

        [TransferFromCompany] NVARCHAR(200)     NULL,
        [TransferToCompany]   NVARCHAR(200)     NULL,
        [TransferStockCardId] INT               NULL,

        [LotId]               INT               NULL,
        [LotImportDeclarNo]   NVARCHAR(50)      NULL,

        [CreatedBy]           NVARCHAR(100)     NULL,
        [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [Remark]              NVARCHAR(500)     NULL,

        CONSTRAINT [PK_imp_stock_m29_card] PRIMARY KEY CLUSTERED ([Id])
    );

    CREATE INDEX [IX_stock_m29_card_material_date] ON [imp].[stock_m29_card] ([RawMaterialCode], [TransactionDate]);
    CREATE INDEX [IX_stock_m29_card_import] ON [imp].[stock_m29_card] ([ImportDeclarNo], [ImportItemNo]);
    CREATE INDEX [IX_stock_m29_card_export] ON [imp].[stock_m29_card] ([ExportDeclarNo], [ExportItemNo]);
    CREATE INDEX [IX_stock_m29_card_privilege] ON [imp].[stock_m29_card] ([PrivilegeType]);
    CREATE INDEX [IX_stock_m29_card_lot] ON [imp].[stock_m29_card] ([LotId]);

    PRINT 'Created table imp.stock_m29_card';
END
GO

-- 3. stock_m29_batch
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_batch')
BEGIN
    CREATE TABLE [imp].[stock_m29_batch] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [StockLotId]          INT               NOT NULL,
        [ExportDeclarNo]      NVARCHAR(50)      NOT NULL,
        [ExportItemNo]        INT               NOT NULL,
        [ExportDate]          DATE              NOT NULL,
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,

        [ProductionFormulaNo] NVARCHAR(50)      NULL,
        [BomDetailNo]         INT               NULL,

        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        [ExportQty]           DECIMAL(18,6)     NOT NULL,
        [Ratio]               DECIMAL(18,6)     NOT NULL,
        [Scrap]               DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyRequired]         DECIMAL(18,6)     NOT NULL,
        [QtyCut]              DECIMAL(18,6)     NOT NULL,

        [DutyPerUnit]         DECIMAL(18,6)     NULL,
        [DutyRefund]          DECIMAL(18,4)     NULL,

        [FOBValueTHB]         DECIMAL(18,4)     NULL,
        [CompensationRate]    DECIMAL(18,4)     NULL,
        [CompensationAmount]  DECIMAL(18,4)     NULL,

        [TransferTableNo]     NVARCHAR(50)      NULL,
        [TransferFromCompany] NVARCHAR(200)     NULL,

        [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'PENDING',

        [CreatedBy]           NVARCHAR(100)     NULL,
        [CreatedDate]         DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [ConfirmedBy]         NVARCHAR(100)     NULL,
        [ConfirmedDate]       DATETIME2(7)      NULL,

        CONSTRAINT [PK_imp_stock_m29_batch] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_stock_m29_batch_lot] FOREIGN KEY ([StockLotId])
            REFERENCES [imp].[stock_m29_lot] ([Id])
    );

    CREATE INDEX [IX_stock_m29_batch_lot] ON [imp].[stock_m29_batch] ([StockLotId]);
    CREATE INDEX [IX_stock_m29_batch_export] ON [imp].[stock_m29_batch] ([ExportDeclarNo], [ExportItemNo]);
    CREATE INDEX [IX_stock_m29_batch_status] ON [imp].[stock_m29_batch] ([Status]);

    PRINT 'Created table imp.stock_m29_batch';
END
GO

-- 4. stock_m29_on_hand
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_on_hand')
BEGIN
    CREATE TABLE [imp].[stock_m29_on_hand] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        [QtyIn]               DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyOut]              DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL DEFAULT 0,

        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyPerUnit]         DECIMAL(18,6)     NULL,

        [LastUpdatedBy]       NVARCHAR(100)     NULL,
        [LastUpdatedDate]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_stock_m29_on_hand] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_stock_m29_on_hand_material] UNIQUE ([RawMaterialCode])
    );

    CREATE INDEX [IX_stock_m29_on_hand_balance] ON [imp].[stock_m29_on_hand] ([RawMaterialCode], [QtyBalance]);

    PRINT 'Created table imp.stock_m29_on_hand';
END
GO
