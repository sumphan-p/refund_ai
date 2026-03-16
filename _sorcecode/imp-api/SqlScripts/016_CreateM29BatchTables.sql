-- =============================================
-- 016: Create M29 Batch Tables (m29_batch_header, m29_batch_item)
-- Batch management for Section 29 — separate from stock_* tables
-- =============================================

-- 1. Batch Header
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'm29_batch_header')
BEGIN
    CREATE TABLE [imp].[m29_batch_header] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [BatchDocNo]      NVARCHAR(20)      NOT NULL,
        [Status]          NVARCHAR(20)      NOT NULL DEFAULT 'DRAFT',  -- DRAFT → PENDING → CONFIRMED / CANCELLED
        [TotalItems]      INT               NOT NULL DEFAULT 0,
        [TotalNetWeight]  DECIMAL(18,2)     NOT NULL DEFAULT 0,
        [TotalFOBTHB]     DECIMAL(18,2)     NOT NULL DEFAULT 0,
        [Remark]          NVARCHAR(500)     NULL,

        -- Audit
        [CreatedBy]       NVARCHAR(100)     NULL,
        [CreatedDate]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),
        [ConfirmedBy]     NVARCHAR(100)     NULL,
        [ConfirmedDate]   DATETIME2(7)      NULL,
        [CancelledBy]     NVARCHAR(100)     NULL,
        [CancelledDate]   DATETIME2(7)      NULL,

        CONSTRAINT [PK_imp_m29_batch_header] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_imp_m29_batch_header_docno] UNIQUE ([BatchDocNo])
    );

    CREATE INDEX [IX_m29_batch_header_status] ON [imp].[m29_batch_header] ([Status]);

    PRINT 'Created table imp.m29_batch_header';
END
GO

-- 2. Batch Item
IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'imp' AND t.name = 'm29_batch_item')
BEGIN
    CREATE TABLE [imp].[m29_batch_item] (
        [Id]              INT IDENTITY(1,1) NOT NULL,
        [BatchHeaderId]   INT               NOT NULL,
        [ExportExcelId]   INT               NOT NULL,
        [ExportDeclarNo]  NVARCHAR(50)      NOT NULL,
        [ExportItemNo]    INT               NOT NULL,
        [ExportDate]      DATE              NULL,
        [LoadingDate]     NVARCHAR(20)      NULL,
        [ProductCode]     NVARCHAR(50)      NULL,
        [Section19BisNo]  NVARCHAR(50)      NULL,
        [NetWeight]       DECIMAL(18,2)     NULL,
        [FOBTHB]          DECIMAL(18,2)     NULL,
        [SortOrder]       INT               NOT NULL DEFAULT 0,

        -- Audit
        [CreatedBy]       NVARCHAR(100)     NULL,
        [CreatedDate]     DATETIME2(7)      NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_imp_m29_batch_item] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_m29_batch_item_header] FOREIGN KEY ([BatchHeaderId])
            REFERENCES [imp].[m29_batch_header] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_m29_batch_item_header] ON [imp].[m29_batch_item] ([BatchHeaderId]);
    CREATE INDEX [IX_m29_batch_item_export] ON [imp].[m29_batch_item] ([ExportDeclarNo], [ExportItemNo]);
    CREATE INDEX [IX_m29_batch_item_exportexcelid] ON [imp].[m29_batch_item] ([ExportExcelId]);

    PRINT 'Created table imp.m29_batch_item';
END
GO
