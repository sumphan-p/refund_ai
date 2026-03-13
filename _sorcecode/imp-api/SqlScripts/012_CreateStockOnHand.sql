-- =============================================
-- 012: Create stock_m29_on_hand table (running balance per raw material)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'stock_m29_on_hand')
BEGIN
    CREATE TABLE [imp].[stock_m29_on_hand] (
        [Id]                  INT IDENTITY(1,1) NOT NULL,
        [RawMaterialCode]     NVARCHAR(50)      NOT NULL,
        [ProductCode]         NVARCHAR(50)      NULL,
        [ProductDescription]  NVARCHAR(500)     NULL,
        [Unit]                NVARCHAR(20)      NOT NULL,

        -- Quantity
        [QtyIn]               DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyOut]              DECIMAL(18,6)     NOT NULL DEFAULT 0,
        [QtyBalance]          DECIMAL(18,6)     NOT NULL DEFAULT 0,

        -- Latest duty info
        [DutyRate]            DECIMAL(18,4)     NULL,
        [DutyPerUnit]         DECIMAL(18,6)     NULL,

        -- Audit
        [LastUpdatedBy]       NVARCHAR(100)     NULL,
        [LastUpdatedDate]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_stock_m29_on_hand] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_stock_m29_on_hand_material] UNIQUE ([RawMaterialCode])
    );

    CREATE INDEX [IX_stock_m29_on_hand_balance] ON [imp].[stock_m29_on_hand] ([RawMaterialCode], [QtyBalance]);

    PRINT 'Created table imp.stock_m29_on_hand';
END
GO
