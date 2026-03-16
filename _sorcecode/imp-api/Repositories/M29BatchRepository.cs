using System.Data;
using Dapper;
using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Repositories;

public class M29BatchRepository : IM29BatchRepository
{
    private readonly IDbConnection _db;

    public M29BatchRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> InsertHeaderAsync(M29BatchHeader header)
    {
        const string sql = @"
            INSERT INTO imp.m29_batch_header (BatchDocNo, Status, TotalItems, TotalNetWeight, TotalFOBTHB, Remark, CreatedBy, CreatedDate)
            VALUES (@BatchDocNo, @Status, @TotalItems, @TotalNetWeight, @TotalFOBTHB, @Remark, @CreatedBy, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await _db.ExecuteScalarAsync<int>(sql, new
        {
            header.BatchDocNo,
            header.Status,
            header.TotalItems,
            header.TotalNetWeight,
            header.TotalFOBTHB,
            header.Remark,
            header.CreatedBy,
        });
    }

    public async Task InsertItemAsync(M29BatchItem item)
    {
        const string sql = @"
            INSERT INTO imp.m29_batch_item (BatchHeaderId, ExportExcelId, ExportDeclarNo, ExportItemNo,
                ExportDate, LoadingDate, ProductCode, Section19BisNo, NetWeight, FOBTHB, SortOrder, CreatedBy, CreatedDate)
            VALUES (@BatchHeaderId, @ExportExcelId, @ExportDeclarNo, @ExportItemNo,
                @ExportDate, @LoadingDate, @ProductCode, @Section19BisNo, @NetWeight, @FOBTHB, @SortOrder, @CreatedBy, SYSUTCDATETIME());";

        await _db.ExecuteAsync(sql, new
        {
            item.BatchHeaderId,
            item.ExportExcelId,
            item.ExportDeclarNo,
            item.ExportItemNo,
            item.ExportDate,
            item.LoadingDate,
            item.ProductCode,
            item.Section19BisNo,
            item.NetWeight,
            item.FOBTHB,
            item.SortOrder,
            item.CreatedBy,
        });
    }

    public async Task<M29BatchHeader?> GetHeaderByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<M29BatchHeader>(
            "SELECT * FROM imp.m29_batch_header WHERE Id = @Id", new { Id = id });
    }

    public async Task<M29BatchHeader?> GetHeaderByDocNoAsync(string batchDocNo)
    {
        return await _db.QuerySingleOrDefaultAsync<M29BatchHeader>(
            "SELECT * FROM imp.m29_batch_header WHERE BatchDocNo = @BatchDocNo", new { BatchDocNo = batchDocNo });
    }

    public async Task<IEnumerable<M29BatchItem>> GetItemsByHeaderIdAsync(int headerId)
    {
        return await _db.QueryAsync<M29BatchItem>(
            @"SELECT bi.*, e.InvoiceNo, e.BuyerName
              FROM imp.m29_batch_item bi
              LEFT JOIN imp.export_excel e ON e.Id = bi.ExportExcelId
              WHERE bi.BatchHeaderId = @HeaderId
              ORDER BY bi.SortOrder, bi.ExportDeclarNo, bi.ExportItemNo",
            new { HeaderId = headerId });
    }

    public async Task<(IEnumerable<BatchListItem> Items, int TotalCount)> SearchAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(batchDocNo))
        {
            conditions.Add("h.BatchDocNo LIKE @BatchDocNo");
            p.Add("BatchDocNo", $"%{batchDocNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("h.Status = @Status");
            p.Add("Status", status.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateFrom))
        {
            conditions.Add("CAST(h.CreatedDate AS DATE) >= @DateFrom");
            p.Add("DateFrom", dateFrom.Trim());
        }
        if (!string.IsNullOrWhiteSpace(dateTo))
        {
            conditions.Add("CAST(h.CreatedDate AS DATE) <= @DateTo");
            p.Add("DateTo", dateTo.Trim());
        }

        var where = conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM imp.m29_batch_header h{where}";
        var totalCount = await _db.ExecuteScalarAsync<int>(countSql, p);

        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var dataSql = $@"
            SELECT
                h.BatchDocNo,
                h.Status,
                h.TotalItems AS ExportItemCount,
                h.TotalNetWeight,
                h.TotalFOBTHB,
                h.Remark,
                h.CreatedBy,
                h.CreatedDate
            FROM imp.m29_batch_header h{where}
            ORDER BY h.CreatedDate DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var items = await _db.QueryAsync<BatchListItem>(dataSql, p);
        return (items, totalCount);
    }

    public async Task UpdateStatusAsync(int id, string status, string? confirmedBy)
    {
        const string sql = @"
            UPDATE imp.m29_batch_header
            SET Status = @Status, ConfirmedBy = @ConfirmedBy, ConfirmedDate = SYSUTCDATETIME()
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new { Id = id, Status = status, ConfirmedBy = confirmedBy });
    }

    public async Task CancelAsync(int id, string? cancelledBy)
    {
        const string sql = @"
            UPDATE imp.m29_batch_header
            SET Status = 'CANCELLED', CancelledBy = @CancelledBy, CancelledDate = SYSUTCDATETIME()
            WHERE Id = @Id";

        await _db.ExecuteAsync(sql, new { Id = id, CancelledBy = cancelledBy });
    }

    public async Task<int> GetMaxRunningNoAsync(string buddhistYearSuffix)
    {
        var pattern = $"%/{buddhistYearSuffix}";
        var result = await _db.ExecuteScalarAsync<string?>(
            @"SELECT TOP 1 BatchDocNo FROM imp.m29_batch_header
              WHERE BatchDocNo LIKE @Pattern
              ORDER BY BatchDocNo DESC",
            new { Pattern = pattern });

        if (result == null) return 0;
        var slashIdx = result.IndexOf('/');
        if (slashIdx > 0 && int.TryParse(result[..slashIdx], out var num)) return num;
        return 0;
    }

    public async Task<M29BatchHeader?> GetLatestHeaderByYearAsync(string buddhistYearSuffix)
    {
        var pattern = $"%/{buddhistYearSuffix}";
        return await _db.QuerySingleOrDefaultAsync<M29BatchHeader>(
            @"SELECT TOP 1 * FROM imp.m29_batch_header
              WHERE BatchDocNo LIKE @Pattern
              ORDER BY BatchDocNo DESC",
            new { Pattern = pattern });
    }
}
