using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Services;

public interface IImportManageService
{
    Task<PagedResponse<ImportManageListItem>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? brand, int page, int pageSize);
    Task<ImportExcel> GetByIdAsync(int id);
    Task UpdateAsync(int id, UpdateImportManageRequest request, string userName);
    Task DeleteAsync(int id);
}
