using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Repositories;

public interface IStockCuttingRepository
{
    Task<int> InsertAsync(StockCutting cutting);
    Task<IEnumerable<StockCutting>> GetByExportAsync(string exportDeclarNo, int exportItemNo);
    Task<StockCutting?> GetByIdAsync(int id);
    Task UpdateStatusAsync(int id, string status, string? confirmedBy);
    Task DeleteByExportAsync(string exportDeclarNo, int exportItemNo);
    Task<string?> GetCuttingStatusForExportAsync(string exportDeclarNo, int exportItemNo);
    Task<int> GetMaxRunningNoAsync(string buddhistYearSuffix);
    Task<int> CountExportItemsByBatchDocNoAsync(string batchDocNo);
    Task<IEnumerable<StockCutting>> GetByBatchDocNoAsync(string batchDocNo);
    Task DeleteByBatchDocNoAsync(string batchDocNo);

    // Batch management
    Task<(IEnumerable<BatchListItem> Items, int TotalCount)> SearchBatchesAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize);
    Task<IEnumerable<StockCutting>> GetBatchDetailAsync(string batchDocNo);
    Task UpdateStatusByBatchDocNoAsync(string batchDocNo, string status, string? confirmedBy);
}
