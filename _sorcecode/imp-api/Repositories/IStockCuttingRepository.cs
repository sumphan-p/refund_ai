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
}
