using imp_api.Models;

namespace imp_api.Repositories;

public interface IImportExcelRepository
{
    Task<int> UpsertBatchAsync(IEnumerable<ImportExcel> records, string userName);
    Task<IEnumerable<ImportExcel>> GetAllAsync(int? limit = null);

    // CRUD for import manage
    Task<ImportExcel?> GetByIdAsync(int id);
    Task<IEnumerable<ImportExcel>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? brand, int page, int pageSize);
    Task<int> CountAsync(string? declarNo, string? invoiceNo, string? productCode, string? brand);
    Task UpdateAsync(int id, ImportExcel record, string userName);
    Task<bool> DeleteAsync(int id);
}
