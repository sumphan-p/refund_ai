using imp_api.DTOs;

namespace imp_api.Services;

public interface IFormulaM29Service
{
    Task<PagedResponse<FormulaM29ListItem>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize);
    Task<FormulaM29Detail> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateFormulaM29Request request, string userName);
    Task UpdateAsync(int id, UpdateFormulaM29Request request, string userName);
    Task DeleteAsync(int id);
}
