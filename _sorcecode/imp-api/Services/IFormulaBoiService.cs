using imp_api.DTOs;

namespace imp_api.Services;

public interface IFormulaBoiService
{
    Task<PagedResponse<FormulaBoiListItem>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize);
    Task<FormulaBoiDetail> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateFormulaBoiRequest request, string userName);
    Task UpdateAsync(int id, UpdateFormulaBoiRequest request, string userName);
    Task DeleteAsync(int id);
}
