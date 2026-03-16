-- =============================================
-- 010: Create Stock Tables (stock_m29_lot, stock_m29_card, stock_m29_batch)
-- =============================================

-- 1. Stock Lot — FIFO queue per raw material
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_lot')
BEGIN
    CREATE TABLE [imp].[stock_m29_lot] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [ImportDeclarNo]      NVARCHAR(50)      NOT NULL,
        [ImportItemNo]        INT               NOT NULL,
        [ImportDate]          DATE              NOT NULL,
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,       -- '19TVIS', 'BOI', 'COMPENSATION', 'TRANSFER'

        -- Raw material
        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        -- Quantity
        [QtyOriginal]         DECIMAL(18,6)     NOT NULL,
        [QtyUsed]             DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL,
        [QtyTransferred]      DECIMAL(18,6)     NOT NULL DEFAULT 0,

        -- Value / Duty
        [UnitPrice]           DECIMAL(18,4)     NULL,
        [CIFValueTHB]         DECIMAL(18,4)     NULL,
        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyPerUnit]         DECIMAL(18,6)     NULL,
        [TotalDutyVAT]        DECIMAL(18,4)     NULL,

        -- Privilege reference
        [ImportTaxIncId]      NVARCHAR(50)      NULL,
        [BOICardNo]           NVARCHAR(100)     NULL,
        [ProductionFormulaNo] NVARCHAR(50)      NULL,

        -- Status
        [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'ACTIVE',
        [ExpiryDate]          DATE              NULL,

        -- Audit
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

-- 2. Stock Card — transaction log (running balance)
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_card')
BEGIN
    CREATE TABLE [imp].[stock_m29_card] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [TransactionDate]     DATE              NOT NULL,
        [TransactionType]     NVARCHAR(20)      NOT NULL,       -- 'IN', 'OUT', 'TRANSFER_OUT', 'TRANSFER_IN'
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,

        -- Import ref
        [ImportDeclarNo]      NVARCHAR(50)      NULL,
        [ImportItemNo]        INT               NULL,
        [ImportDate]          DATE              NULL,

        -- Export ref
        [ExportDeclarNo]      NVARCHAR(50)      NULL,
        [ExportItemNo]        INT               NULL,

        -- Material
        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        -- Quantity
        [QtyIn]               DECIMAL(18,6)     NULL,
        [QtyOut]              DECIMAL(18,6)     NULL,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL,

        -- Value / Duty
        [UnitPrice]           DECIMAL(18,4)     NULL,
        [CIFValueTHB]         DECIMAL(18,4)     NULL,
        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyAmount]          DECIMAL(18,4)     NULL,
        [VATAmount]           DECIMAL(18,4)     NULL,

        -- Privilege reference
        [ImportTaxIncId]      NVARCHAR(50)      NULL,
        [BOICardNo]           NVARCHAR(100)     NULL,
        [ProductionFormulaNo] NVARCHAR(50)      NULL,
        [CompensationNo]      NVARCHAR(50)      NULL,
        [TransferTableNo]     NVARCHAR(50)      NULL,

        -- Transfer
        [TransferFromCompany] NVARCHAR(200)     NULL,
        [TransferToCompany]   NVARCHAR(200)     NULL,
        [TransferStockCardId] INT               NULL,

        -- Lot tracking
        [LotId]               INT               NULL,
        [LotImportDeclarNo]   NVARCHAR(50)      NULL,

        -- Audit
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

-- 3. Stock Cutting — lot-to-export mapping
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_batch')
BEGIN
    CREATE TABLE [imp].[stock_m29_batch] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [StockLotId]          INT               NOT NULL,
        [ExportDeclarNo]      NVARCHAR(50)      NOT NULL,
        [ExportItemNo]        INT               NOT NULL,
        [ExportDate]          DATE              NOT NULL,
        [PrivilegeType]       NVARCHAR(20)      NOT NULL,

        -- Formula
        [ProductionFormulaNo] NVARCHAR(50)      NULL,
        [BomDetailNo]         INT               NULL,

        -- Material
        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        -- Quantity
        [ExportQty]           DECIMAL(18,6)     NOT NULL,
        [Ratio]               DECIMAL(18,6)     NOT NULL,
        [Scrap]               DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyRequired]         DECIMAL(18,6)     NOT NULL,
        [QtyCut]              DECIMAL(18,6)     NOT NULL,

        -- Duty refund (19tvis / transfer)
        [DutyPerUnit]         DECIMAL(18,6)     NULL,
        [DutyRefund]          DECIMAL(18,4)     NULL,

        -- Compensation
        [FOBValueTHB]         DECIMAL(18,4)     NULL,
        [CompensationRate]    DECIMAL(18,4)     NULL,
        [CompensationAmount]  DECIMAL(18,4)     NULL,

        -- Transfer
        [TransferTableNo]     NVARCHAR(50)      NULL,
        [TransferFromCompany] NVARCHAR(200)     NULL,

        -- Status
        [Status]              NVARCHAR(20)      NOT NULL DEFAULT 'PENDING',

        -- Audit
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
