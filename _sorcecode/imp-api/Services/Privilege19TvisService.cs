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

    public Privilege19TvisService(IDbConnection db, IStockLotRepository lotRepo, IStockCuttingRepository cuttingRepo, IBomM29Repository bomRepo)
    {
        _db = db;
        _lotRepo = lotRepo;
        _cuttingRepo = cuttingRepo;
        _bomRepo = bomRepo;
    }

    // =============================================
    // Search exports eligible for Section 19 bis
    // =============================================
    public async Task<PagedResponse<ExportItemForCutting>> SearchExportsAsync(string? declarNo, string? productCode, string? dateFrom, string? dateTo, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var conditions = new List<string> { "e.Section19Bis IS NOT NULL AND e.Section19Bis != ''" };

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
            conditions.Add("e.ReleaseDate >= @DateFrom");
            p.Add("DateFrom", dateFrom.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            conditions.Add("e.ReleaseDate <= @DateTo");
            p.Add("DateTo", dateTo.Trim());
        }

        var where = " WHERE " + string.Join(" AND ", conditions);

        // Count
        var countSql = $"SELECT COUNT(*) FROM imp.export_excel e{where}";
        var totalCount = await _db.ExecuteScalarAsync<int>(countSql, p);

        // Data with cutting status
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var dataSql = $@"
            SELECT e.Id, e.DeclarNo, e.ItemDeclarNo, e.ReleaseDate AS ExportDate,
                   e.ProductCode, e.DescriptionTh1, e.DescriptionEn1,
                   e.QtyDeclar, e.QtyDeclarUnit, e.NetWeight, e.FOBTHB,
                   e.Section19BisNo, e.ImportTaxIncentiveId, e.ImportDeclarNo,
                   (SELECT TOP 1 sc.Status FROM imp.stock_cutting sc
                    WHERE sc.ExportDeclarNo = e.DeclarNo AND sc.ExportItemNo = e.ItemDeclarNo) AS CuttingStatus
            FROM imp.export_excel e{where}
            ORDER BY e.ReleaseDate DESC, e.DeclarNo, e.ItemDeclarNo
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
        var allCuttings = new List<CuttingResultItem>();
        decimal totalDutyRefund = 0;
        decimal totalQtyRequired = 0;
        bool isFullyCut = true;

        foreach (var bom in bomDetails)
        {
            if (string.IsNullOrWhiteSpace(bom.RawMaterialCode)) continue;

            var ratio = bom.Ratio ?? 0;
            var scrap = bom.Scrap ?? 0;
            var qtyRequired = request.ExportQty * (ratio + scrap);
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
                    CreatedBy = userName,
                };

                var cuttingId = await _cuttingRepo.InsertAsync(cutting);

                // Update lot balance
                var newQtyUsed = lot.QtyUsed + cutQty;
                var newQtyBalance = lot.QtyBalance - cutQty;
                var newStatus = newQtyBalance <= 0 ? "DEPLETED" : "ACTIVE";
                await _lotRepo.UpdateQtyAsync(lot.Id, newQtyUsed, newQtyBalance, newStatus);

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
            FROM imp.stock_cutting sc
            JOIN imp.stock_lot sl ON sc.StockLotId = sl.Id
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
            ExportDate = export.ReleaseDate,
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

            await _cuttingRepo.UpdateStatusAsync(c.Id, "CONFIRMED", userName);
        }

        // Insert stock card OUT records
        foreach (var c in cuttings)
        {
            var lot = await _lotRepo.GetByIdAsync(c.StockLotId);
            if (lot == null) continue;

            var cardSql = @"
                INSERT INTO imp.stock_card (TransactionDate, TransactionType, PrivilegeType,
                    ImportDeclarNo, ImportItemNo, ImportDate,
                    ExportDeclarNo, ExportItemNo,
                    RawMaterialCode, ProductCode, ProductDescription, Unit,
                    QtyOut, QtyBalance,
                    DutyAmount, ImportTaxIncId, ProductionFormulaNo,
                    LotId, LotImportDeclarNo, CreatedBy, CreatedDate, Remark)
                VALUES (@TransactionDate, 'OUT', '19TVIS',
                    @ImportDeclarNo, @ImportItemNo, @ImportDate,
                    @ExportDeclarNo, @ExportItemNo,
                    @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                    @QtyOut, @QtyBalance,
                    @DutyAmount, @ImportTaxIncId, @ProductionFormulaNo,
                    @LotId, @LotImportDeclarNo, @CreatedBy, SYSUTCDATETIME(), @Remark)";

            await _db.ExecuteAsync(cardSql, new
            {
                TransactionDate = c.ExportDate,
                lot.ImportDeclarNo,
                lot.ImportItemNo,
                lot.ImportDate,
                c.ExportDeclarNo,
                c.ExportItemNo,
                c.RawMaterialCode,
                lot.ProductCode,
                lot.ProductDescription,
                c.Unit,
                QtyOut = c.QtyCut,
                QtyBalance = lot.QtyBalance,
                DutyAmount = c.DutyRefund,
                lot.ImportTaxIncId,
                c.ProductionFormulaNo,
                LotId = lot.Id,
                LotImportDeclarNo = lot.ImportDeclarNo,
                CreatedBy = userName,
                Remark = $"ตัด stock มาตรา 29 - ใบขนขาออก {c.ExportDeclarNo}/{c.ExportItemNo}",
            });
        }
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

        // Delete cutting records
        await _cuttingRepo.DeleteByExportAsync(exportDeclarNo, exportItemNo);
    }

    // =============================================
    // Sync import_excel → stock_lot
    // =============================================
    public async Task<SyncStockLotResponse> SyncImportToStockLotAsync(string userName)
    {
        // Get import records that have Section 19 bis privilege and not yet in stock_lot
        var sql = @"
            SELECT i.*
            FROM imp.import_excel i
            WHERE i.UsePrivilege IS NOT NULL AND i.UsePrivilege != ''
              AND NOT EXISTS (
                  SELECT 1 FROM imp.stock_lot sl
                  WHERE sl.ImportDeclarNo = i.DeclarNo AND sl.ImportItemNo = i.ItemDeclarNo
              )
            ORDER BY i.StampDateTime ASC, i.DeclarNo, i.ItemDeclarNo";

        var imports = await _db.QueryAsync<ImportExcel>(sql);
        int inserted = 0, skipped = 0;

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

            var lot = new StockLot
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
            };

            await _lotRepo.InsertAsync(lot);
            inserted++;
        }

        return new SyncStockLotResponse { InsertedCount = inserted, SkippedCount = skipped };
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
            DaysRemaining = l.ExpiryDate.HasValue ? Math.Max(0, (l.ExpiryDate.Value - DateTime.UtcNow).Days) : 0,
        });

        return new PagedResponse<StockLotListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
