using System.Data;
using Dapper;
using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Repositories;

public class StockCuttingRepository : IStockCuttingRepository
{
    private readonly IDbConnection _db;

    public StockCuttingRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> InsertAsync(StockCutting cutting)
    {
        const string sql = @"
            INSERT INTO imp.stock_m29_batch (StockLotId, ExportDeclarNo, ExportItemNo, ExportDate, PrivilegeType,
                ProductionFormulaNo, BomDetailNo, RawMaterialCode, Unit,
                ExportQty, Ratio, Scrap, QtyRequired, QtyCut,
                DutyPerUnit, DutyRefund, BatchDocNo, Status, CreatedBy, CreatedDate)
            VALUES (@StockLotId, @ExportDeclarNo, @ExportItemNo, @ExportDate, @PrivilegeType,
                @ProductionFormulaNo, @BomDetailNo, @RawMaterialCode, @Unit,
                @ExportQty, @Ratio, @Scrap, @QtyRequired, @QtyCut,
                @DutyPerUnit, @DutyRefund, @BatchDocNo, 'PENDING', @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await _db.ExecuteScalarAsync<int>(sql, new
        {
            cutting.StockLotId,
            cutting.ExportDeclarNo,
            cutting.ExportItemNo,
            cutting.ExportDate,
            cutting.PrivilegeType,
            cutting.ProductionFormulaNo,
            cutting.BomDetailNo,
            cutting.RawMaterialCode,
            cutting.Unit,
            cutting.ExportQty,
            cutting.Ratio,
            cutting.Scrap,
            cutting.QtyRequired,
            cutting.QtyCut,
            cutting.DutyPerUnit,
            cutting.DutyRefund,
            cutting.BatchDocNo,
            cutting.CreatedBy,
        });
    }

    public async Task<IEnumerable<StockCutting>> GetByExportAsync(string exportDeclarNo, int exportItemNo)
    {
        const string sql = @"
            SELECT sc.*, sl.ImportDeclarNo, sl.ImportItemNo, sl.ImportDate
            FROM imp.stock_m29_batch sc
            JOIN imp.stock_m29_lot sl ON sc.StockLotId = sl.Id
            WHERE sc.ExportDeclarNo = @ExportDeclarNo AND sc.ExportItemNo = @ExportItemNo
            ORDER BY sl.ImportDate ASC";

        return await _db.QueryAsync<StockCutting>(sql, new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }

    public async Task<StockCutting?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<StockCutting>(
            "SELECT * FROM imp.stock_m29_batch WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateStatusAsync(int id, string status, string? confirmedBy)
    {
        const string sql = @"
            UPDATE imp.stock_m29_batch SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedDate = SYSUTCDATETIME()
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new { Id = id, Status = status, ConfirmedBy = confirmedBy });
    }

    public async Task DeleteByExportAsync(string exportDeclarNo, int exportItemNo)
    {
        await _db.ExecuteAsync(
            "DELETE FROM imp.stock_m29_batch WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }

    public async Task<string?> GetCuttingStatusForExportAsync(string exportDeclarNo, int exportItemNo)
    {
        return await _db.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 Status FROM imp.stock_m29_batch WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }

    public async Task<int> GetMaxRunningNoAsync(string buddhistYearSuffix)
    {
        var pattern = $"%/{buddhistYearSuffix}";
        var result = await _db.ExecuteScalarAsync<string?>(
            @"SELECT TOP 1 BatchDocNo FROM imp.stock_m29_batch
              WHERE BatchDocNo LIKE @Pattern
              ORDER BY BatchDocNo DESC",
            new { Pattern = pattern });

        if (result == null) return 0;
        var slashIdx = result.IndexOf('/');
        if (slashIdx > 0 && int.TryParse(result[..slashIdx], out var num)) return num;
        return 0;
    }

    public async Task<int> CountExportItemsByBatchDocNoAsync(string batchDocNo)
    {
        return await _db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(DISTINCT CONCAT(ExportDeclarNo, '-', ExportItemNo))
              FROM imp.stock_m29_batch
              WHERE BatchDocNo = @BatchDocNo",
            new { BatchDocNo = batchDocNo });
    }

    public async Task<IEnumerable<StockCutting>> GetByBatchDocNoAsync(string batchDocNo)
    {
        return await _db.QueryAsync<StockCutting>(
            "SELECT * FROM imp.stock_m29_batch WHERE BatchDocNo = @BatchDocNo",
            new { BatchDocNo = batchDocNo });
    }

    public async Task DeleteByBatchDocNoAsync(string batchDocNo)
    {
        await _db.ExecuteAsync(
            "DELETE FROM imp.stock_m29_batch WHERE BatchDocNo = @BatchDocNo",
            new { BatchDocNo = batchDocNo });
    }

    // =============================================
    // Batch management
    // =============================================

    public async Task<(IEnumerable<BatchListItem> Items, int TotalCount)> SearchBatchesAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(batchDocNo))
        {
            conditions.Add("sc.BatchDocNo LIKE @BatchDocNo");
            p.Add("BatchDocNo", $"%{batchDocNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("sc.Status = @Status");
            p.Add("Status", status.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            conditions.Add("CAST(sc.CreatedDate AS DATE) >= @DateFrom");
            p.Add("DateFrom", dateFrom.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            conditions.Add("CAST(sc.CreatedDate AS DATE) <= @DateTo");
            p.Add("DateTo", dateTo.Trim());
        }

        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";

        // Wrap in CTE to group by BatchDocNo
        var countSql = $@"
            SELECT COUNT(DISTINCT sc.BatchDocNo) FROM imp.stock_m29_batch sc{where}";
        var totalCount = await _db.ExecuteScalarAsync<int>(countSql, p);

        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var dataSql = $@"
            SELECT
                sc.BatchDocNo,
                CASE WHEN MIN(sc.Status) = MAX(sc.Status) THEN MIN(sc.Status)
                     WHEN MIN(sc.Status) = 'PENDING' THEN 'PENDING'
                     ELSE MIN(sc.Status) END AS Status,
                COUNT(DISTINCT CONCAT(sc.ExportDeclarNo, '-', sc.ExportItemNo)) AS ExportItemCount,
                SUM(sc.QtyCut) AS TotalQtyCut,
                SUM(ISNULL(sc.DutyRefund, 0)) AS TotalDutyRefund,
                MIN(sc.CreatedBy) AS CreatedBy,
                MIN(sc.CreatedDate) AS CreatedDate
            FROM imp.stock_m29_batch sc{where}
            GROUP BY sc.BatchDocNo
            ORDER BY MIN(sc.CreatedDate) DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await _db.QueryAsync<BatchListItem>(dataSql, p);
        return (items, totalCount);
    }

    public async Task<IEnumerable<StockCutting>> GetBatchDetailAsync(string batchDocNo)
    {
        const string sql = @"
            SELECT sc.*, sl.ImportDeclarNo, sl.ImportItemNo, sl.ImportDate
            FROM imp.stock_m29_batch sc
            JOIN imp.stock_m29_lot sl ON sc.StockLotId = sl.Id
            WHERE sc.BatchDocNo = @BatchDocNo
            ORDER BY sc.ExportDeclarNo, sc.ExportItemNo, sl.ImportDate ASC";

        return await _db.QueryAsync<StockCutting>(sql, new { BatchDocNo = batchDocNo });
    }

    public async Task UpdateStatusByBatchDocNoAsync(string batchDocNo, string status, string? confirmedBy)
    {
        const string sql = @"
            UPDATE imp.stock_m29_batch
            SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedDate = SYSUTCDATETIME()
            WHERE BatchDocNo = @BatchDocNo AND Status = 'PENDING'";

        await _db.ExecuteAsync(sql, new { BatchDocNo = batchDocNo, Status = status, ConfirmedBy = confirmedBy });
    }
}
