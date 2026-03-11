using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Services;

public interface IExportManageService
{
    Task<PagedResponse<ExportManageListItem>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? buyerName, int page, int pageSize);
    Task<ExportExcel> GetByIdAsync(int id);
    Task UpdateAsync(int id, UpdateExportManageRequest request, string userName);
    Task DeleteAsync(int id);
}
