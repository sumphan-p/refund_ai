using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class StockLotRepository : IStockLotRepository
{
    private readonly IDbConnection _db;

    public StockLotRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<StockLot>> GetActiveLotsFifoAsync(string rawMaterialCode, string privilegeType, DateTime exportDate)
    {
        const string sql = @"
            SELECT * FROM imp.stock_m29_lot
            WHERE RawMaterialCode = @RawMaterialCode
              AND PrivilegeType = @PrivilegeType
              AND Status = 'ACTIVE'
              AND (ExpiryDate IS NULL OR ExpiryDate >= @ExportDate)
              AND DATEDIFF(DAY, ImportDate, GETUTCDATE()) <= 365
            ORDER BY ImportDate ASC";

        return await _db.QueryAsync<StockLot>(sql, new { RawMaterialCode = rawMaterialCode, PrivilegeType = privilegeType, ExportDate = exportDate });
    }

    public async Task<IEnumerable<StockLot>> GetAllActiveLotsFifoAsync(string rawMaterialCode, string privilegeType)
    {
        const string sql = @"
            SELECT * FROM imp.stock_m29_lot
            WHERE RawMaterialCode = @RawMaterialCode
              AND PrivilegeType = @PrivilegeType
              AND Status = 'ACTIVE'
            ORDER BY ImportDate ASC";

        return await _db.QueryAsync<StockLot>(sql, new { RawMaterialCode = rawMaterialCode, PrivilegeType = privilegeType });
    }

    public async Task<StockLot?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<StockLot>(
            "SELECT * FROM imp.stock_m29_lot WHERE Id = @Id", new { Id = id });
    }

    public async Task<StockLot?> GetByImportDeclarAsync(string importDeclarNo, int importItemNo)
    {
        return await _db.QuerySingleOrDefaultAsync<StockLot>(
            "SELECT * FROM imp.stock_m29_lot WHERE ImportDeclarNo = @ImportDeclarNo AND ImportItemNo = @ImportItemNo",
            new { ImportDeclarNo = importDeclarNo, ImportItemNo = importItemNo });
    }

    public async Task<int> InsertAsync(StockLot lot)
    {
        const string sql = @"
            INSERT INTO imp.stock_m29_lot (ImportDeclarNo, ImportItemNo, ImportDate, PrivilegeType,
                RawMaterialCode, ProductCode, ProductDescription, Unit,
                QtyOriginal, QtyUsed, QtyBalance, QtyTransferred,
                UnitPrice, CIFValueTHB, DutyRate, DutyPerUnit, TotalDutyVAT,
                ImportTaxIncId, BOICardNo, ProductionFormulaNo,
                Status, ExpiryDate, CreatedBy, CreatedDate)
            VALUES (@ImportDeclarNo, @ImportItemNo, @ImportDate, @PrivilegeType,
                @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                @QtyOriginal, 0, @QtyBalance, 0,
                @UnitPrice, @CIFValueTHB, @DutyRate, @DutyPerUnit, @TotalDutyVAT,
                @ImportTaxIncId, @BOICardNo, @ProductionFormulaNo,
                'ACTIVE', @ExpiryDate, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await _db.ExecuteScalarAsync<int>(sql, new
        {
            lot.ImportDeclarNo,
            lot.ImportItemNo,
            lot.ImportDate,
            lot.PrivilegeType,
            lot.RawMaterialCode,
            lot.ProductCode,
            lot.ProductDescription,
            lot.Unit,
            lot.QtyOriginal,
            QtyBalance = lot.QtyOriginal,
            lot.UnitPrice,
            lot.CIFValueTHB,
            lot.DutyRate,
            lot.DutyPerUnit,
            lot.TotalDutyVAT,
            lot.ImportTaxIncId,
            lot.BOICardNo,
            lot.ProductionFormulaNo,
            lot.ExpiryDate,
            lot.CreatedBy,
        });
    }

    public async Task UpdateQtyAsync(int id, decimal qtyUsed, decimal qtyBalance, string status)
    {
        const string sql = @"
            UPDATE imp.stock_m29_lot SET QtyUsed = @QtyUsed, QtyBalance = @QtyBalance, Status = @Status
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new { Id = id, QtyUsed = qtyUsed, QtyBalance = qtyBalance, Status = status });
    }

    public async Task<IEnumerable<StockLot>> SearchAsync(string? importDeclarNo, string? rawMaterialCode, string? privilegeType, string? status, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var where = BuildWhere(importDeclarNo, rawMaterialCode, privilegeType, status, p);
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var sql = $@"SELECT * FROM imp.stock_m29_lot{where}
                     ORDER BY ImportDate ASC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return await _db.QueryAsync<StockLot>(sql, p);
    }

    public async Task<int> CountAsync(string? importDeclarNo, string? rawMaterialCode, string? privilegeType, string? status)
    {
        var p = new DynamicParameters();
        var where = BuildWhere(importDeclarNo, rawMaterialCode, privilegeType, status, p);
        var sql = $"SELECT COUNT(*) FROM imp.stock_m29_lot{where}";
        return await _db.ExecuteScalarAsync<int>(sql, p);
    }

    private static string BuildWhere(string? importDeclarNo, string? rawMaterialCode, string? privilegeType, string? status, DynamicParameters p)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(importDeclarNo))
        {
            conditions.Add("ImportDeclarNo LIKE @ImportDeclarNo");
            p.Add("ImportDeclarNo", $"%{importDeclarNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(rawMaterialCode))
        {
            conditions.Add("RawMaterialCode LIKE @RawMaterialCode");
            p.Add("RawMaterialCode", $"%{rawMaterialCode.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(privilegeType))
        {
            conditions.Add("PrivilegeType = @PrivilegeType");
            p.Add("PrivilegeType", privilegeType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("Status = @Status");
            p.Add("Status", status.Trim());
        }
        return conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
    }
}
