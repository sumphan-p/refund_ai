using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class BomM29Repository : IBomM29Repository
{
    private readonly IDbConnection _db;

    public BomM29Repository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BomM29Hd>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(formulaNo, description, productType, p);
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var sql = $@"SELECT h.*, (SELECT COUNT(*) FROM imp.bom_m29_dt WHERE BomM29HdId = h.Id) AS DetailCount
                     FROM imp.bom_m29_hd h{where}
                     ORDER BY h.ProductionFormulaNo ASC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        return await _db.QueryAsync<BomM29Hd>(sql, p);
    }

    public async Task<int> CountAsync(string? formulaNo, string? description, string? productType)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(formulaNo, description, productType, p);
        var sql = $"SELECT COUNT(*) FROM imp.bom_m29_hd h{where}";
        return await _db.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task<BomM29Hd?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<BomM29Hd>(
            "SELECT * FROM imp.bom_m29_hd WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<BomM29Hd?> GetByFormulaNoAsync(string formulaNo)
    {
        return await _db.QuerySingleOrDefaultAsync<BomM29Hd>(
            "SELECT * FROM imp.bom_m29_hd WHERE ProductionFormulaNo = @FormulaNo",
            new { FormulaNo = formulaNo });
    }

    public async Task<IEnumerable<BomM29Dt>> GetDetailsByHdIdAsync(int hdId)
    {
        return await _db.QueryAsync<BomM29Dt>(
            "SELECT * FROM imp.bom_m29_dt WHERE BomM29HdId = @HdId ORDER BY [No]",
            new { HdId = hdId });
    }

    public async Task<int> InsertHdAsync(BomM29Hd hd)
    {
        const string sql = @"
            INSERT INTO imp.bom_m29_hd (ProductionFormulaNo, DescriptionEn1, DescriptionTh1, ProductType, CreatedBy, CreatedDate)
            VALUES (@ProductionFormulaNo, @DescriptionEn1, @DescriptionTh1, @ProductType, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await _db.ExecuteScalarAsync<int>(sql, new
        {
            hd.ProductionFormulaNo,
            hd.DescriptionEn1,
            hd.DescriptionTh1,
            hd.ProductType,
            hd.CreatedBy,
        });
    }

    public async Task UpdateHdAsync(int id, BomM29Hd hd)
    {
        const string sql = @"
            UPDATE imp.bom_m29_hd SET
                DescriptionEn1 = @DescriptionEn1,
                DescriptionTh1 = @DescriptionTh1,
                ProductType = @ProductType,
                ModifiedBy = @ModifiedBy,
                ModifiedDate = SYSUTCDATETIME()
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new
        {
            Id = id,
            hd.DescriptionEn1,
            hd.DescriptionTh1,
            hd.ProductType,
            hd.ModifiedBy,
        });
    }

    public async Task DeleteDetailsByHdIdAsync(int hdId)
    {
        await _db.ExecuteAsync(
            "DELETE FROM imp.bom_m29_dt WHERE BomM29HdId = @HdId",
            new { HdId = hdId });
    }

    public async Task InsertDetailAsync(BomM29Dt dt)
    {
        const string sql = @"
            INSERT INTO imp.bom_m29_dt (BomM29HdId, [No], RawMaterialCode, ProductType, Unit, Ratio, Scrap, Remark, CreatedBy, CreatedDate)
            VALUES (@BomM29HdId, @No, @RawMaterialCode, @ProductType, @Unit, @Ratio, @Scrap, @Remark, @CreatedBy, SYSUTCDATETIME())";

        await _db.ExecuteAsync(sql, new
        {
            dt.BomM29HdId,
            dt.No,
            dt.RawMaterialCode,
            dt.ProductType,
            dt.Unit,
            dt.Ratio,
            dt.Scrap,
            dt.Remark,
            dt.CreatedBy,
        });
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affected = await _db.ExecuteAsync(
            "DELETE FROM imp.bom_m29_hd WHERE Id = @Id",
            new { Id = id });
        return affected > 0;
    }

    private static string BuildWhereClause(string? formulaNo, string? description, string? productType, DynamicParameters p)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(formulaNo))
        {
            conditions.Add("h.ProductionFormulaNo LIKE @FormulaNo");
            p.Add("FormulaNo", $"%{formulaNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(description))
        {
            conditions.Add("(h.DescriptionTh1 LIKE @Description OR h.DescriptionEn1 LIKE @Description)");
            p.Add("Description", $"%{description.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(productType))
        {
            conditions.Add("h.ProductType LIKE @ProductType");
            p.Add("ProductType", $"%{productType.Trim()}%");
        }
        return conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
    }
}
