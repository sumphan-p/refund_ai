using System.Data;
using Dapper;
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
            INSERT INTO imp.stock_cutting (StockLotId, ExportDeclarNo, ExportItemNo, ExportDate, PrivilegeType,
                ProductionFormulaNo, BomDetailNo, RawMaterialCode, Unit,
                ExportQty, Ratio, Scrap, QtyRequired, QtyCut,
                DutyPerUnit, DutyRefund, Status, CreatedBy, CreatedDate)
            VALUES (@StockLotId, @ExportDeclarNo, @ExportItemNo, @ExportDate, @PrivilegeType,
                @ProductionFormulaNo, @BomDetailNo, @RawMaterialCode, @Unit,
                @ExportQty, @Ratio, @Scrap, @QtyRequired, @QtyCut,
                @DutyPerUnit, @DutyRefund, 'PENDING', @CreatedBy, SYSUTCDATETIME());
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
            cutting.CreatedBy,
        });
    }

    public async Task<IEnumerable<StockCutting>> GetByExportAsync(string exportDeclarNo, int exportItemNo)
    {
        const string sql = @"
            SELECT sc.*, sl.ImportDeclarNo, sl.ImportItemNo, sl.ImportDate
            FROM imp.stock_cutting sc
            JOIN imp.stock_lot sl ON sc.StockLotId = sl.Id
            WHERE sc.ExportDeclarNo = @ExportDeclarNo AND sc.ExportItemNo = @ExportItemNo
            ORDER BY sl.ImportDate ASC";

        return await _db.QueryAsync<StockCutting>(sql, new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }

    public async Task<StockCutting?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<StockCutting>(
            "SELECT * FROM imp.stock_cutting WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateStatusAsync(int id, string status, string? confirmedBy)
    {
        const string sql = @"
            UPDATE imp.stock_cutting SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedDate = SYSUTCDATETIME()
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new { Id = id, Status = status, ConfirmedBy = confirmedBy });
    }

    public async Task DeleteByExportAsync(string exportDeclarNo, int exportItemNo)
    {
        await _db.ExecuteAsync(
            "DELETE FROM imp.stock_cutting WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }

    public async Task<string?> GetCuttingStatusForExportAsync(string exportDeclarNo, int exportItemNo)
    {
        return await _db.ExecuteScalarAsync<string?>(
            "SELECT TOP 1 Status FROM imp.stock_cutting WHERE ExportDeclarNo = @ExportDeclarNo AND ExportItemNo = @ExportItemNo",
            new { ExportDeclarNo = exportDeclarNo, ExportItemNo = exportItemNo });
    }
}
