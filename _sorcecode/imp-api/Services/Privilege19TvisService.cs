using System.Data;
using Dapper;
using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class Privilege19TvisService : IPrivilege19TvisService
{
    private readonly IDbConnection _db;
    private readonly IStockLotRepository _lotRepo;
    private readonly IStockCuttingRepository _cuttingRepo;
    private readonly IBomM29Repository _bomRepo;
    private readonly IM29BatchRepository _m29BatchRepo;

    public Privilege19TvisService(IDbConnection db, IStockLotRepository lotRepo, IStockCuttingRepository cuttingRepo, IBomM29Repository bomRepo, IM29BatchRepository m29BatchRepo)
    {
        _db = db;
        _lotRepo = lotRepo;
        _cuttingRepo = cuttingRepo;
        _bomRepo = bomRepo;
        _m29BatchRepo = m29BatchRepo;
    }

    // =============================================
    // Search exports eligible for Section 19 bis
    // =============================================
    public async Task<PagedResponse<ExportItemForCutting>> SearchExportsAsync(string? declarNo, string? productCode, string? dateFrom, string? dateTo, bool uncutOnly, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var conditions = new List<string> { "e.Section19Bis = N'Y' AND e.Section19BisNo <> N'00000000-00' AND e.Section19BisNo <> N'' AND e.CurrentStatus = N'0409'" };

        if (!string.IsNullOrWhiteSpace(declarNo))
        {
            conditions.Add("e.DeclarNo LIKE @DeclarNo");
            p.Add("DeclarNo", $"%{declarNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            conditions.Add("e.ProductCode LIKE @ProductCode");
            p.Add("ProductCode", $"%{productCode.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            conditions.Add("TRY_CONVERT(DATE, e.LoadingDate, 103) >= @DateFrom");
            p.Add("DateFrom", dateFrom.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            conditions.Add("TRY_CONVERT(DATE, e.LoadingDate, 103) <= @DateTo");
            p.Add("DateTo", dateTo.Trim());
        }

        var joinSql = @"
            LEFT JOIN (
                SELECT bi.ExportExcelId
                FROM imp.m29_batch_item bi
                INNER JOIN imp.m29_batch_header bh ON bh.Id = bi.BatchHeaderId
                WHERE bh.Status <> N'CANCELLED'
            ) bi ON bi.ExportExcelId = e.Id";

        // uncutOnly: กรองเฉพาะรายการที่ยังไม่ถูกจัดชุด (server-side)
        if (uncutOnly)
            conditions.Add("bi.ExportExcelId IS NULL");

        var where = " WHERE " + string.Join(" AND ", conditions);

        // Count
        var countSql = $"SELECT COUNT(*) FROM imp.export_excel e{joinSql}{where}";
        var totalCount = await _db.ExecuteScalarAsync<int>(countSql, p);

        // Data
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var dataSql = $@"
            SELECT e.Id, e.DeclarNo, e.ItemDeclarNo, e.LoadingDate AS ExportDate,
                   e.ProductCode, e.DescriptionTh1, e.DescriptionEn1,
                   e.QtyDeclar, e.QtyDeclarUnit, e.NetWeight, e.FOBTHB,
                   e.InvoiceNo, e.BuyerName,
                   e.Section19BisNo, e.ImportTaxIncentiveId, e.ImportDeclarNo,
                   CASE WHEN bi.ExportExcelId IS NOT NULL THEN N'BATCHED' END AS CuttingStatus
            FROM imp.export_excel e{joinSql}{where}
            ORDER BY TRY_CONVERT(DATE, e.LoadingDate, 103) DESC, e.DeclarNo, e.ItemDeclarNo
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await _db.QueryAsync<ExportItemForCutting>(dataSql, p);

        return new PagedResponse<ExportItemForCutting>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    // =============================================
    // Calculate FIFO cutting (preview + save as PENDING)
    // =============================================
    public async Task<CutStock19TvisResponse> CalculateFifoAsync(CutStock19TvisRequest request, string userName)
    {
        // Validate: not already cut
        var existingStatus = await _cuttingRepo.GetCuttingStatusForExportAsync(request.ExportDeclarNo, request.ExportItemNo);
        if (existingStatus != null)
            throw new AppException("ALREADY_CUT", "รายการส่งออกนี้ได้ตัด stock แล้ว");

        // Get BOM formula
        var bomHd = await _bomRepo.GetByFormulaNoAsync(request.ProductionFormulaNo.Trim())
            ?? throw new AppException("FORMULA_NOT_FOUND", $"ไม่พบสูตรการผลิต: {request.ProductionFormulaNo}");

        var bomDetails = await _bomRepo.GetDetailsByHdIdAsync(bomHd.Id);
        if (!bomDetails.Any())
            throw new AppException("FORMULA_EMPTY", "สูตรการผลิตไม่มีรายการวัตถุดิบ");

        var exportDate = DateTime.Parse(request.ExportDate);

        // Generate batchDocNo: "001/69" format (running/พ.ศ. 2 หลัก)
        var buddhistYear = (DateTime.UtcNow.Year + 543).ToString();
        var yearSuffix = buddhistYear[^2..]; // last 2 digits
        int runningNo;
        if (request.StartDocRunning.HasValue && request.StartDocRunning.Value > 0)
        {
            runningNo = request.StartDocRunning.Value;
        }
        else
        {
            var maxRunning = await _cuttingRepo.GetMaxRunningNoAsync(yearSuffix);
            runningNo = maxRunning + 1;
        }
        var batchDocNo = $"{runningNo:D3}/{yearSuffix}";

        // Business rule: max 11 export items per batch
        var existingCount = await _cuttingRepo.CountExportItemsByBatchDocNoAsync(batchDocNo);
        if (existingCount >= 10)
            throw new AppException("BATCH_LIMIT", "1 ชุดเอกสาร ต้องมีใบขนขาออกไม่เกิน 11 รายการ");

        var allCuttings = new List<CuttingResultItem>();
        decimal totalDutyRefund = 0;
        decimal totalQtyRequired = 0;
        bool isFullyCut = true;

        foreach (var bom in bomDetails)
        {
            if (string.IsNullOrWhiteSpace(bom.RawMaterialCode)) continue;

            var ratio = bom.Ratio ?? 0;
            var scrap = bom.Scrap ?? 0;
            var qtyRequired = request.ExportQty * (ratio + scrap) / 100;
            totalQtyRequired += qtyRequired;

            // Get FIFO lots
            var lots = await _lotRepo.GetActiveLotsFifoAsync(bom.RawMaterialCode, "19TVIS", exportDate);
            var remaining = qtyRequired;

            foreach (var lot in lots)
            {
                if (remaining <= 0) break;

                var cutQty = Math.Min(remaining, lot.QtyBalance);
                var dutyRefund = cutQty * (lot.DutyPerUnit ?? 0);

                // Save cutting record as PENDING
                var cutting = new StockCutting
                {
                    StockLotId = lot.Id,
                    ExportDeclarNo = request.ExportDeclarNo,
                    ExportItemNo = request.ExportItemNo,
                    ExportDate = exportDate,
                    PrivilegeType = "19TVIS",
                    ProductionFormulaNo = request.ProductionFormulaNo,
                    BomDetailNo = bom.No,
                    RawMaterialCode = bom.RawMaterialCode,
                    Unit = bom.Unit ?? request.ExportUnit,
                    ExportQty = request.ExportQty,
                    Ratio = ratio,
                    Scrap = scrap,
                    QtyRequired = qtyRequired,
                    QtyCut = cutQty,
                    DutyPerUnit = lot.DutyPerUnit,
                    DutyRefund = dutyRefund,
                    BatchDocNo = batchDocNo,
                    CreatedBy = userName,
                };

                var cuttingId = await _cuttingRepo.InsertAsync(cutting);

                // Update lot balance
                var newQtyUsed = lot.QtyUsed + cutQty;
                var newQtyBalance = lot.QtyBalance - cutQty;
                var newStatus = newQtyBalance <= 0 ? "DEPLETED" : "ACTIVE";
                await _lotRepo.UpdateQtyAsync(lot.Id, newQtyUsed, newQtyBalance, newStatus);

                // Insert stock_m29_batch record
                await _db.ExecuteAsync(@"
                    INSERT INTO imp.stock_m29_batch
                        (StockLotId, ExportDeclarNo, ExportItemNo, ExportDate,
                         PrivilegeType, ProductionFormulaNo, BomDetailNo,
                         RawMaterialCode, Unit, ExportQty, Ratio, Scrap,
                         QtyRequired, QtyCut, DutyPerUnit, DutyRefund,
                         Status, CreatedBy, CreatedDate)
                    VALUES
                        (@StockLotId, @ExportDeclarNo, @ExportItemNo, @ExportDate,
                         @PrivilegeType, @ProductionFormulaNo, @BomDetailNo,
                         @RawMaterialCode, @Unit, @ExportQty, @Ratio, @Scrap,
                         @QtyRequired, @QtyCut, @DutyPerUnit, @DutyRefund,
                         @Status, @CreatedBy, SYSUTCDATETIME())",
                    new {
                        cutting.StockLotId, cutting.ExportDeclarNo, cutting.ExportItemNo,
                        ExportDate = exportDate, cutting.PrivilegeType,
                        cutting.ProductionFormulaNo, cutting.BomDetailNo,
                        cutting.RawMaterialCode, cutting.Unit, cutting.ExportQty,
                        cutting.Ratio, cutting.Scrap, cutting.QtyRequired,
                        QtyCut = cutQty, lot.DutyPerUnit, DutyRefund = dutyRefund,
                        Status = "PENDING", CreatedBy = userName
                    });

                // Insert stock_m29_card OUT record
                await _db.ExecuteAsync(@"
                    INSERT INTO imp.stock_m29_card
                        (TransactionDate, TransactionType, PrivilegeType,
                         ImportDeclarNo, ImportItemNo, ImportDate,
                         ExportDeclarNo, ExportItemNo,
                         RawMaterialCode, ProductCode, ProductDescription, Unit,
                         QtyOut, QtyBalance,
                         DutyAmount, ImportTaxIncId, ProductionFormulaNo,
                         LotId, LotImportDeclarNo, CreatedBy, CreatedDate, Remark)
                    VALUES
                        (@TransactionDate, 'OUT', '19TVIS',
                         @ImportDeclarNo, @ImportItemNo, @ImportDate,
                         @ExportDeclarNo, @ExportItemNo,
                         @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                         @QtyOut, @QtyBalance,
                         @DutyAmount, @ImportTaxIncId, @ProductionFormulaNo,
                         @LotId, @LotImportDeclarNo, @CreatedBy, SYSUTCDATETIME(), @Remark)",
                    new {
                        TransactionDate = exportDate,
                        lot.ImportDeclarNo, lot.ImportItemNo, lot.ImportDate,
                        cutting.ExportDeclarNo, cutting.ExportItemNo,
                        cutting.RawMaterialCode,
                        lot.ProductCode, lot.ProductDescription,
                        cutting.Unit,
                        QtyOut = cutQty,
                        QtyBalance = newQtyBalance,
                        DutyAmount = dutyRefund,
                        lot.ImportTaxIncId,
                        cutting.ProductionFormulaNo,
                        LotId = lot.Id,
                        LotImportDeclarNo = lot.ImportDeclarNo,
                        CreatedBy = userName,
                        Remark = $"FIFO cut: {request.ExportDeclarNo}-{request.ExportItemNo}"
                    });

                allCuttings.Add(new CuttingResultItem
                {
                    CuttingId = cuttingId,
                    LotId = lot.Id,
                    ImportDeclarNo = lot.ImportDeclarNo,
                    ImportItemNo = lot.ImportItemNo,
                    ImportDate = lot.ImportDate.ToString("yyyy-MM-dd"),
                    RawMaterialCode = bom.RawMaterialCode,
                    Unit = bom.Unit ?? "",
                    QtyCut = cutQty,
                    DutyPerUnit = lot.DutyPerUnit,
                    DutyRefund = dutyRefund,
                    LotBalanceAfter = newQtyBalance,
                });

                totalDutyRefund += dutyRefund;
                remaining -= cutQty;
            }

            if (remaining > 0)
                isFullyCut = false;
        }

        return new CutStock19TvisResponse
        {
            BatchDocNo = batchDocNo,
            Cuttings = allCuttings,
            TotalQtyRequired = totalQtyRequired,
            TotalQtyCut = allCuttings.Sum(c => c.QtyCut),
            TotalDutyRefund = totalDutyRefund,
            IsFullyCut = isFullyCut,
        };
    }

    // =============================================
    // Get cutting detail for an export item
    // =============================================
    public async Task<ExportCuttingDetail> GetCuttingDetailAsync(string exportDeclarNo, int exportItemNo)
    {
        // Get export info
        var export = await _db.QuerySingleOrDefaultAsync<ExportExcel>(
            "SELECT * FROM imp.export_excel WHERE DeclarNo = @DeclarNo AND ItemDeclarNo = @ItemDeclarNo",
            new { DeclarNo = exportDeclarNo, ItemDeclarNo = exportItemNo })
            ?? throw new AppException("NOT_FOUND", "ไม่พบข้อมูลใบขนขาออก");

        // Get cutting records with lot info
        var cuttings = await _db.QueryAsync<dynamic>(@"
            SELECT sc.Id AS CuttingId, sc.StockLotId AS LotId,
                   sl.ImportDeclarNo, sl.ImportItemNo, sl.ImportDate,
                   sc.RawMaterialCode, sc.Unit, sc.QtyCut,
                   sc.DutyPerUnit, sc.DutyRefund, sl.QtyBalance AS LotBalanceAfter,
                   sc.Status
            FROM imp.stock_m29_batch sc
            JOIN imp.stock_m29_lot sl ON sc.StockLotId = sl.Id
            WHERE sc.ExportDeclarNo = @ExportDeclarNo AND sc.ExportItemNo = @ExportItemNo
            ORDER BY sl.ImportDate ASC",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });

        var items = cuttings.Select(c => new CuttingResultItem
        {
            CuttingId = (int)c.CuttingId,
            LotId = (int)c.LotId,
            ImportDeclarNo = (string)c.ImportDeclarNo,
            ImportItemNo = (int)c.ImportItemNo,
            ImportDate = ((DateTime)c.ImportDate).ToString("yyyy-MM-dd"),
            RawMaterialCode = (string)c.RawMaterialCode,
            Unit = (string)c.Unit,
            QtyCut = (decimal)c.QtyCut,
            DutyPerUnit = (decimal?)c.DutyPerUnit,
            DutyRefund = (decimal?)c.DutyRefund,
            LotBalanceAfter = (decimal)c.LotBalanceAfter,
        }).ToList();

        var status = items.FirstOrDefault() != null
            ? (await _cuttingRepo.GetCuttingStatusForExportAsync(exportDeclarNo, exportItemNo) ?? "PENDING")
            : "NONE";

        return new ExportCuttingDetail
        {
            ExportDeclarNo = exportDeclarNo,
            ExportItemNo = exportItemNo,
            ExportDate = export.LoadingDate,
            ProductCode = export.ProductCode,
            ExportQty = export.QtyDeclar,
            ProductionFormulaNo = null,
            Status = status,
            TotalDutyRefund = items.Sum(c => c.DutyRefund ?? 0),
            Cuttings = items,
        };
    }

    // =============================================
    // Confirm cutting (PENDING → CONFIRMED)
    // =============================================
    public async Task ConfirmCuttingAsync(string exportDeclarNo, int exportItemNo, string userName)
    {
        var cuttings = await _cuttingRepo.GetByExportAsync(exportDeclarNo, exportItemNo);
        if (!cuttings.Any())
            throw new AppException("NOT_FOUND", "ไม่พบรายการตัด stock");

        foreach (var c in cuttings)
        {
            if (c.Status != "PENDING")
                throw new AppException("INVALID_STATUS", $"รายการตัด #{c.Id} ไม่อยู่ในสถานะรอยืนยัน");

            // Update stock_m29_batch status
            await _cuttingRepo.UpdateStatusAsync(c.Id, "CONFIRMED", userName);
        }

        // Update stock_m29_batch status
        await _db.ExecuteAsync(
            @"UPDATE imp.stock_m29_batch SET Status = 'CONFIRMED', ConfirmedBy = @UserName, ConfirmedDate = SYSUTCDATETIME()
              WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo AND Status = 'PENDING'",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo, UserName = userName });
    }

    // =============================================
    // Cancel cutting (PENDING → restore lot qty)
    // =============================================
    public async Task CancelCuttingAsync(string exportDeclarNo, int exportItemNo, string userName)
    {
        var cuttings = await _cuttingRepo.GetByExportAsync(exportDeclarNo, exportItemNo);
        if (!cuttings.Any())
            throw new AppException("NOT_FOUND", "ไม่พบรายการตัด stock");

        foreach (var c in cuttings)
        {
            if (c.Status != "PENDING")
                throw new AppException("INVALID_STATUS", "สามารถยกเลิกได้เฉพาะรายการที่อยู่ในสถานะรอยืนยัน");

            // Restore lot balance
            var lot = await _lotRepo.GetByIdAsync(c.StockLotId);
            if (lot != null)
            {
                var restoredUsed = lot.QtyUsed - c.QtyCut;
                var restoredBalance = lot.QtyBalance + c.QtyCut;
                await _lotRepo.UpdateQtyAsync(lot.Id, restoredUsed, restoredBalance, "ACTIVE");
            }
        }

        // Delete from stock_m29_batch
        await _db.ExecuteAsync(
            "DELETE FROM imp.stock_m29_batch WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });

        // Delete from stock_m29_card
        await _db.ExecuteAsync(
            "DELETE FROM imp.stock_m29_card WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo AND TransactionType = 'OUT'",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });

        // Delete cutting records from stock_m29_batch
        await _cuttingRepo.DeleteByExportAsync(exportDeclarNo, exportItemNo);
    }

    // =============================================
    // Cancel all cuttings by BatchDocNo
    // =============================================
    public async Task<int> CancelByBatchDocNoAsync(string batchDocNo, string userName)
    {
        var cuttings = await _cuttingRepo.GetByBatchDocNoAsync(batchDocNo);
        var list = cuttings.ToList();
        if (list.Count == 0)
            throw new AppException("NOT_FOUND", $"ไม่พบรายการตัด stock เลขที่เอกสาร: {batchDocNo}");

        foreach (var c in list)
        {
            if (c.Status == "CONFIRMED")
                throw new AppException("INVALID_STATUS", $"ไม่สามารถยกเลิกเอกสาร {batchDocNo} เนื่องจากมีรายการยืนยันแล้ว");
        }

        // Restore all lot balances
        foreach (var c in list)
        {
            var lot = await _lotRepo.GetByIdAsync(c.StockLotId);
            if (lot != null)
            {
                var restoredUsed = lot.QtyUsed - c.QtyCut;
                var restoredBalance = lot.QtyBalance + c.QtyCut;
                await _lotRepo.UpdateQtyAsync(lot.Id, restoredUsed, restoredBalance, "ACTIVE");
            }
        }

        // Delete from stock_m29_batch by batch export items
        foreach (var c in list)
        {
            await _db.ExecuteAsync(
                "DELETE FROM imp.stock_m29_batch WHERE StockLotId = @StockLotId AND ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
                new { c.StockLotId, c.ExportDeclarNo, c.ExportItemNo });
            await _db.ExecuteAsync(
                "DELETE FROM imp.stock_m29_card WHERE LotId = @LotId AND ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo AND TransactionType = 'OUT'",
                new { LotId = c.StockLotId, c.ExportDeclarNo, c.ExportItemNo });
        }

        // Delete all cutting records from stock_m29_batch for this batch
        await _cuttingRepo.DeleteByBatchDocNoAsync(batchDocNo);
        return list.Count;
    }

    // =============================================
    // Sync import_excel → stock_m29_lot
    // =============================================
    public async Task<SyncStockLotResponse> SyncImportToStockLotAsync(string userName)
    {
        // Get ALL import records that have Section 19 bis privilege
        var sql = @"
            SELECT i.*
            FROM imp.import_excel i
            WHERE i.UsePrivilege IS NOT NULL AND i.UsePrivilege != ''
            ORDER BY i.StampDateTime ASC, i.DeclarNo, i.ItemDeclarNo";

        var imports = await _db.QueryAsync<ImportExcel>(sql);
        int inserted = 0, updated = 0, skipped = 0;

        foreach (var imp in imports)
        {
            // Determine quantity
            var qty = imp.NetWeight ?? imp.Quantity ?? 0;
            if (qty <= 0) { skipped++; continue; }

            var unit = imp.QtyUnit ?? imp.QuantityUnit ?? "KG";
            var importDate = DateTime.TryParse(imp.StampDateTime, out var dt) ? dt : DateTime.UtcNow;

            // Calculate DutyPerUnit
            decimal? dutyPerUnit = null;
            if (imp.TotalDutyVAT.HasValue && imp.TotalDutyVAT > 0 && qty > 0)
                dutyPerUnit = imp.TotalDutyVAT.Value / qty;
            else if (imp.DutyTextTHB.HasValue && imp.DutyTextTHB > 0 && qty > 0)
                dutyPerUnit = imp.DutyTextTHB.Value / qty;

            // MERGE: insert new lots, update existing lots (preserve QtyUsed)
            var result = await _db.ExecuteScalarAsync<string>(@"
                DECLARE @action NVARCHAR(10);
                MERGE imp.stock_m29_lot AS target
                USING (SELECT @ImportDeclarNo AS ImportDeclarNo, @ImportItemNo AS ImportItemNo) AS source
                ON target.ImportDeclarNo = source.ImportDeclarNo AND target.ImportItemNo = source.ImportItemNo
                WHEN MATCHED THEN
                    UPDATE SET
                        ImportDate = @ImportDate,
                        RawMaterialCode = @RawMaterialCode,
                        ProductCode = @ProductCode,
                        ProductDescription = @ProductDescription,
                        Unit = @Unit,
                        QtyOriginal = @QtyOriginal,
                        QtyBalance = @QtyOriginal - target.QtyUsed,
                        UnitPrice = @UnitPrice,
                        CIFValueTHB = @CIFValueTHB,
                        DutyRate = @DutyRate,
                        DutyPerUnit = @DutyPerUnit,
                        TotalDutyVAT = @TotalDutyVAT,
                        ImportTaxIncId = @ImportTaxIncId,
                        BOICardNo = @BOICardNo,
                        ProductionFormulaNo = @ProductionFormulaNo,
                        ExpiryDate = @ExpiryDate
                WHEN NOT MATCHED THEN
                    INSERT (ImportDeclarNo, ImportItemNo, ImportDate, PrivilegeType,
                            RawMaterialCode, ProductCode, ProductDescription, Unit,
                            QtyOriginal, QtyBalance,
                            UnitPrice, CIFValueTHB, DutyRate, DutyPerUnit, TotalDutyVAT,
                            ImportTaxIncId, BOICardNo, ProductionFormulaNo,
                            Status, ExpiryDate, CreatedBy)
                    VALUES (@ImportDeclarNo, @ImportItemNo, @ImportDate, @PrivilegeType,
                            @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                            @QtyOriginal, @QtyBalance,
                            @UnitPrice, @CIFValueTHB, @DutyRate, @DutyPerUnit, @TotalDutyVAT,
                            @ImportTaxIncId, @BOICardNo, @ProductionFormulaNo,
                            'ACTIVE', @ExpiryDate, @CreatedBy)
                OUTPUT $action;",
                new
                {
                    ImportDeclarNo = imp.DeclarNo,
                    ImportItemNo = imp.ItemDeclarNo,
                    ImportDate = importDate,
                    PrivilegeType = "19TVIS",
                    RawMaterialCode = imp.MaterialCode ?? imp.ProductCode ?? imp.DeclarNo,
                    ProductCode = imp.ProductCode,
                    ProductDescription = imp.DescriptionTh1 ?? imp.DescriptionEn1,
                    Unit = unit,
                    QtyOriginal = qty,
                    QtyBalance = qty,
                    UnitPrice = imp.UnitPrice,
                    CIFValueTHB = imp.CIFTHB,
                    DutyRate = imp.DutyRate,
                    DutyPerUnit = dutyPerUnit,
                    TotalDutyVAT = imp.TotalDutyVAT,
                    ImportTaxIncId = imp.ImportTaxInc,
                    BOICardNo = imp.BOICardNo,
                    ProductionFormulaNo = imp.ProductionFormula,
                    ExpiryDate = importDate.AddYears(1),
                    CreatedBy = userName,
                });

            if (result == "INSERT") inserted++;
            else if (result == "UPDATE") updated++;
        }

        return new SyncStockLotResponse { InsertedCount = inserted, UpdatedCount = updated, SkippedCount = skipped };
    }

    // =============================================
    // Search stock lots
    // =============================================
    public async Task<PagedResponse<StockLotListItem>> SearchLotsAsync(string? importDeclarNo, string? rawMaterialCode, string? status, int page, int pageSize)
    {
        var totalCount = await _lotRepo.CountAsync(importDeclarNo, rawMaterialCode, "19TVIS", status);
        var lots = await _lotRepo.SearchAsync(importDeclarNo, rawMaterialCode, "19TVIS", status, page, pageSize);

        var items = lots.Select(l => new StockLotListItem
        {
            Id = l.Id,
            ImportDeclarNo = l.ImportDeclarNo,
            ImportItemNo = l.ImportItemNo,
            ImportDate = l.ImportDate.ToString("yyyy-MM-dd"),
            RawMaterialCode = l.RawMaterialCode,
            ProductDescription = l.ProductDescription,
            Unit = l.Unit,
            QtyOriginal = l.QtyOriginal,
            QtyUsed = l.QtyUsed,
            QtyBalance = l.QtyBalance,
            DutyPerUnit = l.DutyPerUnit,
            Status = l.Status,
            ExpiryDate = l.ExpiryDate?.ToString("yyyy-MM-dd"),
            AgeDays = (DateTime.UtcNow - l.ImportDate).Days,
        });

        return new PagedResponse<StockLotListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    // =============================================
    // Get export lines by exact DeclarNo
    // =============================================
    public async Task<List<ExportLineItem>> GetExportLinesByDeclarNoAsync(string declarNo)
    {
        var sql = @"
            SELECT e.Id, e.DeclarNo, e.ItemDeclarNo, e.LoadingDate AS ExportDate,
                   e.InvoiceNo, e.ProductCode, e.DescriptionTh1,
                   e.QtyDeclar, e.QtyDeclarUnit, e.NetWeight, e.FOBTHB,
                   e.ImportDeclarNo, e.Section19BisNo, e.ImportTaxIncentiveId,
                   CASE WHEN bi.ExportExcelId IS NOT NULL THEN N'BATCHED' END AS CuttingStatus
            FROM imp.export_excel e
            LEFT JOIN (
                SELECT bi2.ExportExcelId
                FROM imp.m29_batch_item bi2
                INNER JOIN imp.m29_batch_header bh ON bh.Id = bi2.BatchHeaderId
                WHERE bh.Status <> N'CANCELLED'
            ) bi ON bi.ExportExcelId = e.Id
            WHERE e.DeclarNo = @DeclarNo
              AND e.Section19Bis = N'Y'
              AND e.Section19BisNo <> N'00000000-00'
              AND e.Section19BisNo <> N''
              AND e.CurrentStatus = N'0409'
            ORDER BY e.ItemDeclarNo";

        var items = await _db.QueryAsync<ExportLineItem>(sql, new { DeclarNo = declarNo.Trim() });
        return items.ToList();
    }

    // =============================================
    // Get BOM formula info with calculated quantities
    // =============================================
    public async Task<BomFormulaInfo> GetBomFormulaAsync(string formulaNo, decimal exportQty)
    {
        var bomHd = await _bomRepo.GetByFormulaNoAsync(formulaNo.Trim())
            ?? throw new AppException("FORMULA_NOT_FOUND", $"ไม่พบสูตรการผลิต: {formulaNo}");

        var bomDetails = await _bomRepo.GetDetailsByHdIdAsync(bomHd.Id);

        // Query on-hand balance for each distinct raw material
        var materialCodes = bomDetails
            .Where(d => !string.IsNullOrWhiteSpace(d.RawMaterialCode))
            .Select(d => d.RawMaterialCode!)
            .Distinct()
            .ToList();

        var onHandMap = new Dictionary<string, decimal>();
        if (materialCodes.Count > 0)
        {
            var onHandRows = await _db.QueryAsync<(string RawMaterialCode, decimal QtyBalance)>(
                @"SELECT RawMaterialCode, SUM(QtyBalance) AS QtyBalance
                  FROM imp.stock_m29_lot
                  WHERE RawMaterialCode IN @Codes
                    AND PrivilegeType = '19TVIS'
                    AND Status = 'ACTIVE'
                  GROUP BY RawMaterialCode",
                new { Codes = materialCodes });
            foreach (var row in onHandRows)
                onHandMap[row.RawMaterialCode] = row.QtyBalance;
        }

        return new BomFormulaInfo
        {
            ProductionFormulaNo = bomHd.ProductionFormulaNo,
            DescriptionTh1 = bomHd.DescriptionTh1,
            Details = bomDetails.Select(d => new BomFormulaDetail
            {
                No = d.No,
                RawMaterialCode = d.RawMaterialCode,
                ProductType = d.ProductType,
                Unit = d.Unit,
                Ratio = d.Ratio,
                Scrap = d.Scrap,
                QtyFromFormula = exportQty * (d.Ratio ?? 0) / 100,
                QtyRequired = exportQty * ((d.Ratio ?? 0) + (d.Scrap ?? 0)) / 100,
                QtyOnHand = !string.IsNullOrWhiteSpace(d.RawMaterialCode) && onHandMap.TryGetValue(d.RawMaterialCode, out var bal) ? bal : null,
                Remark = d.Remark,
            }).ToList(),
        };
    }

    // =============================================
    // Get available lots for a material (FIFO order)
    // =============================================
    public async Task<List<StockLotListItem>> GetAvailableLotsForMaterialAsync(string rawMaterialCode)
    {
        var lots = await _lotRepo.GetAllActiveLotsFifoAsync(rawMaterialCode, "19TVIS");

        return lots.Select(l => new StockLotListItem
        {
            Id = l.Id,
            ImportDeclarNo = l.ImportDeclarNo,
            ImportItemNo = l.ImportItemNo,
            ImportDate = l.ImportDate.ToString("yyyy-MM-dd"),
            RawMaterialCode = l.RawMaterialCode,
            ProductDescription = l.ProductDescription,
            Unit = l.Unit,
            QtyOriginal = l.QtyOriginal,
            QtyUsed = l.QtyUsed,
            QtyBalance = l.QtyBalance,
            DutyPerUnit = l.DutyPerUnit,
            Status = l.Status,
            ExpiryDate = l.ExpiryDate?.ToString("yyyy-MM-dd"),
            AgeDays = (DateTime.UtcNow - l.ImportDate).Days,
        }).ToList();
    }

    // =============================================
    // Get stock card by material
    // =============================================
    public async Task<List<StockCardEntry>> GetStockCardByMaterialAsync(string rawMaterialCode)
    {
        var sql = @"
            SELECT Id, TransactionType, TransactionDate, ImportDeclarNo, ImportItemNo, ImportDate,
                   PrivilegeType, ExportDeclarNo, ExportItemNo,
                   RawMaterialCode, ProductCode, Unit, QtyIn, QtyOut, QtyBalance
            FROM imp.stock_m29_card
            WHERE RawMaterialCode = @RawMaterialCode AND PrivilegeType = '19TVIS'
            ORDER BY Id DESC";

        var cards = await _db.QueryAsync<dynamic>(sql, new { RawMaterialCode = rawMaterialCode });

        return cards.Select(c => new StockCardEntry
        {
            Id = (int)c.Id,
            TransactionType = (string?)c.TransactionType,
            TransactionDate = c.TransactionDate != null ? ((DateTime)c.TransactionDate).ToString("yyyy-MM-dd") : null,
            ImportDeclarNo = (string?)c.ImportDeclarNo,
            ImportItemNo = (int?)c.ImportItemNo,
            ImportDate = c.ImportDate != null ? ((DateTime)c.ImportDate).ToString("yyyy-MM-dd") : null,
            PrivilegeType = (string?)c.PrivilegeType,
            ExportDeclarNo = (string?)c.ExportDeclarNo,
            ExportItemNo = (int?)c.ExportItemNo,
            RawMaterialCode = (string?)c.RawMaterialCode,
            ProductCode = (string?)c.ProductCode,
            Unit = (string)c.Unit,
            QtyIn = (decimal?)c.QtyIn,
            QtyOut = (decimal?)c.QtyOut,
            QtyBalance = (decimal)c.QtyBalance,
        }).ToList();
    }

    // =============================================
    // Get next cutting document number
    // =============================================
    public async Task<NextDocNoResponse> GetNextDocNoAsync()
    {
        var buddhistYear = (DateTime.UtcNow.Year + 543).ToString();
        var yearSuffix = buddhistYear[^2..];
        var maxRunning = await _cuttingRepo.GetMaxRunningNoAsync(yearSuffix);
        var nextRunning = maxRunning + 1;

        return new NextDocNoResponse
        {
            NextRunning = nextRunning,
            YearSuffix = yearSuffix,
            NextDocNo = $"{nextRunning:D3}/{yearSuffix}",
        };
    }

    // =============================================
    // Batch Management
    // =============================================

    public async Task<PagedResponse<BatchListItem>> SearchBatchesAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize)
    {
        var (items, totalCount) = await _cuttingRepo.SearchBatchesAsync(batchDocNo, status, dateFrom, dateTo, page, pageSize);

        return new PagedResponse<BatchListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<BatchDetailResponse> GetBatchDetailAsync(string batchDocNo)
    {
        var cuttings = (await _cuttingRepo.GetBatchDetailAsync(batchDocNo)).ToList();
        if (cuttings.Count == 0)
            throw new AppException("NOT_FOUND", $"ไม่พบชุดเอกสาร: {batchDocNo}");

        var first = cuttings.First();

        // Group by (ExportDeclarNo, ExportItemNo)
        var exportItems = cuttings
            .GroupBy(c => new { c.ExportDeclarNo, c.ExportItemNo })
            .Select(g => new BatchExportItemDetail
            {
                ExportDeclarNo = g.Key.ExportDeclarNo,
                ExportItemNo = g.Key.ExportItemNo,
                ExportDate = g.First().ExportDate.ToString("yyyy-MM-dd"),
                ProductionFormulaNo = g.First().ProductionFormulaNo,
                ExportQty = g.First().ExportQty,
                TotalQtyCut = g.Sum(c => c.QtyCut),
                TotalDutyRefund = g.Sum(c => c.DutyRefund ?? 0),
                Status = g.First().Status,
                Cuttings = g.Select(c => new CuttingResultItem
                {
                    CuttingId = c.Id,
                    LotId = c.StockLotId,
                    ImportDeclarNo = c.ImportDeclarNo ?? "",
                    ImportItemNo = c.ImportItemNo,
                    ImportDate = c.ImportDate?.ToString("yyyy-MM-dd"),
                    RawMaterialCode = c.RawMaterialCode,
                    Unit = c.Unit,
                    QtyCut = c.QtyCut,
                    DutyPerUnit = c.DutyPerUnit,
                    DutyRefund = c.DutyRefund,
                    LotBalanceAfter = 0, // not relevant for batch detail view
                }).ToList(),
            }).ToList();

        // Determine overall status
        var allStatuses = cuttings.Select(c => c.Status).Distinct().ToList();
        var overallStatus = allStatuses.Count == 1 ? allStatuses[0] : "PENDING";

        return new BatchDetailResponse
        {
            BatchDocNo = batchDocNo,
            Status = overallStatus,
            CreatedBy = first.CreatedBy,
            CreatedDate = first.CreatedDate,
            ConfirmedBy = first.ConfirmedBy,
            ConfirmedDate = first.ConfirmedDate,
            TotalQtyCut = cuttings.Sum(c => c.QtyCut),
            TotalDutyRefund = cuttings.Sum(c => c.DutyRefund ?? 0),
            ExportItems = exportItems,
        };
    }

    public async Task ConfirmBatchAsync(string batchDocNo, string userName)
    {
        var cuttings = (await _cuttingRepo.GetByBatchDocNoAsync(batchDocNo)).ToList();
        if (cuttings.Count == 0)
            throw new AppException("NOT_FOUND", $"ไม่พบชุดเอกสาร: {batchDocNo}");

        // Validate all are PENDING
        if (cuttings.Any(c => c.Status != "PENDING"))
            throw new AppException("INVALID_STATUS", "ชุดเอกสารนี้มีรายการที่ไม่อยู่ในสถานะรอยืนยัน");

        // Update stock_m29_batch status
        await _cuttingRepo.UpdateStatusByBatchDocNoAsync(batchDocNo, "CONFIRMED", userName);

        // Update stock_m29_batch status
        await _db.ExecuteAsync(
            @"UPDATE imp.stock_m29_batch SET Status = 'CONFIRMED', ConfirmedBy = @UserName, ConfirmedDate = SYSUTCDATETIME()
              WHERE ExportDeclarNo IN (SELECT DISTINCT ExportDeclarNo FROM imp.stock_m29_batch WHERE BatchDocNo = @BatchDocNo)
                AND ExportItemNo IN (SELECT DISTINCT ExportItemNo FROM imp.stock_m29_batch WHERE BatchDocNo = @BatchDocNo)
                AND Status = 'PENDING'",
            new { BatchDocNo = batchDocNo, UserName = userName });

        // Update m29_batch_header status
        await _db.ExecuteAsync(
            @"UPDATE imp.m29_batch_header SET Status = 'CONFIRMED', ConfirmedBy = @UserName, ConfirmedDate = SYSUTCDATETIME()
              WHERE BatchDocNo = @BatchDocNo AND Status = 'PENDING'",
            new { BatchDocNo = batchDocNo, UserName = userName });
    }

    // =============================================
    // M29 Batch Management (m29_batch_header / m29_batch_item)
    // =============================================

    public async Task<PagedResponse<BatchListItem>> SearchM29BatchesAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize)
    {
        var (items, totalCount) = await _m29BatchRepo.SearchAsync(batchDocNo, status, dateFrom, dateTo, page, pageSize);
        return new PagedResponse<BatchListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<NextDocNoResponse> GetNextM29BatchDocNoAsync()
    {
        var buddhistYear = (DateTime.UtcNow.Year + 543).ToString();
        var yearSuffix = buddhistYear[^2..];

        // ดึงชุดเอกสารล่าสุดของปีนี้
        var latestHeader = await _m29BatchRepo.GetLatestHeaderByYearAsync(yearSuffix);

        if (latestHeader != null)
        {
            // ชุดล่าสุดยังไม่ตัด stock สมบูรณ์ → ใช้เลขเดิม (ไม่ running ถัดไป)
            if (latestHeader.Status != "CONFIRMED")
            {
                return new NextDocNoResponse
                {
                    NextRunning = 0,
                    YearSuffix = yearSuffix,
                    NextDocNo = latestHeader.BatchDocNo,
                    Remark = latestHeader.Remark,
                };
            }
        }

        // ชุดล่าสุด CONFIRMED แล้ว หรือยังไม่มีชุดในปีนี้ → running ถัดไป
        // ถ้าขึ้นปีใหม่ (ไม่มี record ในปีนี้) maxRunning = 0 → nextRunning = 1
        var maxRunning = await _m29BatchRepo.GetMaxRunningNoAsync(yearSuffix);
        var nextRunning = maxRunning + 1;

        return new NextDocNoResponse
        {
            NextRunning = nextRunning,
            YearSuffix = yearSuffix,
            NextDocNo = $"{nextRunning:D3}/{yearSuffix}",
        };
    }

    public async Task<CreateBatchResponse> CreateM29BatchAsync(CreateBatchRequest request, string userName)
    {
        if (request.ExportExcelIds.Count == 0)
            throw new AppException("VALIDATION_ERROR", "กรุณาเลือกรายการส่งออก");

        // Validate: ชุดล่าสุดต้อง CONFIRMED ก่อนสร้างชุดใหม่
        var buddhistYear = (DateTime.UtcNow.Year + 543).ToString();
        var yearSuffix = buddhistYear[^2..];
        var latestHeader = await _m29BatchRepo.GetLatestHeaderByYearAsync(yearSuffix);
        if (latestHeader != null && latestHeader.Status != "CONFIRMED")
            throw new AppException("BATCH_NOT_COMPLETED", $"ชุดเอกสาร {latestHeader.BatchDocNo} ยังไม่ได้ตัด stock สมบูรณ์ กรุณายืนยันชุดเดิมก่อนสร้างชุดใหม่");

        // Fetch export items
        var exports = (await _db.QueryAsync<ExportExcel>(
            "SELECT * FROM imp.export_excel WHERE Id IN @Ids",
            new { Ids = request.ExportExcelIds })).ToList();

        if (exports.Count == 0)
            throw new AppException("NOT_FOUND", "ไม่พบรายการส่งออกที่เลือก");

        // Validate: ตรวจว่า item ถูกจัดชุดแล้วหรือยัง (ป้องกัน race condition)
        var alreadyBatched = await _db.QueryAsync<int>(
            @"SELECT bi.ExportExcelId FROM imp.m29_batch_item bi
              INNER JOIN imp.m29_batch_header bh ON bh.Id = bi.BatchHeaderId
              WHERE bh.Status <> N'CANCELLED' AND bi.ExportExcelId IN @Ids",
            new { Ids = request.ExportExcelIds });
        if (alreadyBatched.Any())
        {
            var batchedDeclars = exports.Where(e => alreadyBatched.Contains(e.Id))
                .Select(e => e.DeclarNo).Distinct();
            throw new AppException("ALREADY_BATCHED",
                $"รายการใบขน {string.Join(", ", batchedDeclars)} ถูกจัดชุดแล้ว กรุณาปิดและเปิดใหม่");
        }

        // Validate: distinct DeclarNo ≤ 10
        var distinctDeclars = exports.Select(e => e.DeclarNo).Distinct().ToList();
        if (distinctDeclars.Count > 10)
            throw new AppException("BATCH_LIMIT", "1 ชุดเอกสาร ต้องมีใบขนขาออกไม่เกิน 10 ฉบับ");

        // Validate: total FOBTHB ≤ 10,000,000
        var totalFob = exports.Sum(e => e.FOBTHB ?? 0);
        if (totalFob > 10_000_000)
            throw new AppException("FOB_LIMIT", "มูลค่า FOB รวมต้องไม่เกิน 10,000,000 บาท");

        var totalNetWeight = exports.Sum(e => e.NetWeight ?? 0);

        // Auto-generate remark summary
        var remark = $"เลือกแล้ว {exports.Count} รายการ ({distinctDeclars.Count} ใบขน) · น้ำหนัก: {totalNetWeight:#,##0.00} kg · FOB: {totalFob:#,##0.00} บาท";

        // Generate doc no
        var docNoInfo = await GetNextM29BatchDocNoAsync();

        // Insert header + items ใน transaction — ถ้าไม่สำเร็จ rollback ทั้งหมด
        if (_db.State != System.Data.ConnectionState.Open)
            _db.Open();
        using var transaction = _db.BeginTransaction();
        try
        {
            // Insert header
            var header = new M29BatchHeader
            {
                BatchDocNo = docNoInfo.NextDocNo,
                Status = "PENDING",
                TotalItems = exports.Count,
                TotalNetWeight = totalNetWeight,
                TotalFOBTHB = totalFob,
                Remark = remark,
                CreatedBy = userName,
            };
            var headerId = await _db.ExecuteScalarAsync<int>(
                @"INSERT INTO imp.m29_batch_header (BatchDocNo, Status, TotalItems, TotalNetWeight, TotalFOBTHB, Remark, CreatedBy, CreatedDate)
                  VALUES (@BatchDocNo, @Status, @TotalItems, @TotalNetWeight, @TotalFOBTHB, @Remark, @CreatedBy, SYSUTCDATETIME());
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { header.BatchDocNo, header.Status, header.TotalItems, header.TotalNetWeight, header.TotalFOBTHB, header.Remark, header.CreatedBy },
                transaction);

            // Insert items sorted by LoadingDate ASC
            var sorted = exports.OrderBy(e =>
                DateTime.TryParseExact(e.LoadingDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.MaxValue
            ).ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                var exp = sorted[i];
                DateTime? exportDate = DateTime.TryParseExact(exp.LoadingDate, "dd/MM/yyyy", null,
                    System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;

                await _db.ExecuteAsync(
                    @"INSERT INTO imp.m29_batch_item (BatchHeaderId, ExportExcelId, ExportDeclarNo, ExportItemNo,
                        ExportDate, LoadingDate, ProductCode, Section19BisNo, NetWeight, FOBTHB, SortOrder, CreatedBy, CreatedDate)
                      VALUES (@BatchHeaderId, @ExportExcelId, @ExportDeclarNo, @ExportItemNo,
                        @ExportDate, @LoadingDate, @ProductCode, @Section19BisNo, @NetWeight, @FOBTHB, @SortOrder, @CreatedBy, SYSUTCDATETIME());",
                    new
                    {
                        BatchHeaderId = headerId,
                        ExportExcelId = exp.Id,
                        ExportDeclarNo = exp.DeclarNo,
                        ExportItemNo = exp.ItemDeclarNo,
                        ExportDate = exportDate,
                        LoadingDate = exp.LoadingDate,
                        ProductCode = exp.ProductCode,
                        Section19BisNo = exp.Section19BisNo,
                        NetWeight = exp.NetWeight,
                        FOBTHB = exp.FOBTHB,
                        SortOrder = i + 1,
                        CreatedBy = userName,
                    },
                    transaction);
            }

            transaction.Commit();

            return new CreateBatchResponse
            {
                BatchHeaderId = headerId,
                BatchDocNo = docNoInfo.NextDocNo,
                TotalItems = exports.Count,
                TotalNetWeight = totalNetWeight,
                TotalFOBTHB = totalFob,
                Remark = remark,
            };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new AppException("SAVE_ERROR", $"บันทึกชุดเอกสารไม่สำเร็จ: {ex.Message}");
        }
    }

    public async Task<M29BatchDetailResponse> GetM29BatchDetailAsync(string batchDocNo)
    {
        var header = await _m29BatchRepo.GetHeaderByDocNoAsync(batchDocNo)
            ?? throw new AppException("NOT_FOUND", $"ไม่พบชุดเอกสาร: {batchDocNo}");

        var items = await _m29BatchRepo.GetItemsByHeaderIdAsync(header.Id);

        return new M29BatchDetailResponse
        {
            BatchDocNo = header.BatchDocNo,
            Status = header.Status,
            TotalItems = header.TotalItems,
            TotalNetWeight = header.TotalNetWeight,
            TotalFOBTHB = header.TotalFOBTHB,
            CreatedBy = header.CreatedBy,
            CreatedDate = header.CreatedDate,
            ConfirmedBy = header.ConfirmedBy,
            ConfirmedDate = header.ConfirmedDate,
            CancelledBy = header.CancelledBy,
            CancelledDate = header.CancelledDate,
            Items = items.Select(i => new M29BatchItemDetail
            {
                Id = i.Id,
                ExportExcelId = i.ExportExcelId,
                ExportDeclarNo = i.ExportDeclarNo,
                ExportItemNo = i.ExportItemNo,
                ExportDate = i.ExportDate?.ToString("yyyy-MM-dd"),
                LoadingDate = i.LoadingDate,
                ProductCode = i.ProductCode,
                Section19BisNo = i.Section19BisNo,
                NetWeight = i.NetWeight,
                FOBTHB = i.FOBTHB,
                SortOrder = i.SortOrder,
                InvoiceNo = i.InvoiceNo,
                BuyerName = i.BuyerName,
            }).ToList(),
        };
    }

    public async Task ConfirmM29BatchAsync(string batchDocNo, string userName)
    {
        var header = await _m29BatchRepo.GetHeaderByDocNoAsync(batchDocNo)
            ?? throw new AppException("NOT_FOUND", $"ไม่พบชุดเอกสาร: {batchDocNo}");

        if (header.Status != "PENDING")
            throw new AppException("INVALID_STATUS", "ชุดเอกสารนี้ไม่อยู่ในสถานะรอยืนยัน");

        await _m29BatchRepo.UpdateStatusAsync(header.Id, "CONFIRMED", userName);
    }

    public async Task CancelM29BatchAsync(string batchDocNo, string userName)
    {
        var header = await _m29BatchRepo.GetHeaderByDocNoAsync(batchDocNo)
            ?? throw new AppException("NOT_FOUND", $"ไม่พบชุดเอกสาร: {batchDocNo}");

        if (header.Status == "CONFIRMED")
            throw new AppException("INVALID_STATUS", "ไม่สามารถยกเลิกชุดเอกสารที่ยืนยันแล้ว");

        // DELETE header → items ถูกลบตาม (CASCADE)
        await _db.ExecuteAsync(
            "DELETE FROM imp.m29_batch_header WHERE Id = @Id",
            new { header.Id });
    }
}
