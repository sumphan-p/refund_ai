using imp_api.Models;

namespace imp_api.Repositories;

public interface IExportExcelRepository
{
    Task<int> UpsertBatchAsync(IEnumerable<ExportExcel> records, string userName);
    Task<IEnumerable<ExportExcel>> GetAllAsync(int? limit = null);

    // CRUD for export manage
    Task<ExportExcel?> GetByIdAsync(int id);
    Task<IEnumerable<ExportExcel>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? buyerName, int page, int pageSize);
    Task<int> CountAsync(string? declarNo, string? invoiceNo, string? productCode, string? buyerName);
    Task UpdateAsync(int id, ExportExcel record, string userName);
    Task<bool> DeleteAsync(int id);
}
