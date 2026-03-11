using imp_api.Models;

namespace imp_api.Repositories;

public interface IBomM29Repository
{
    Task<IEnumerable<BomM29Hd>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize);
    Task<int> CountAsync(string? formulaNo, string? description, string? productType);
    Task<BomM29Hd?> GetByIdAsync(int id);
    Task<BomM29Hd?> GetByFormulaNoAsync(string formulaNo);
    Task<IEnumerable<BomM29Dt>> GetDetailsByHdIdAsync(int hdId);
    Task<int> InsertHdAsync(BomM29Hd hd);
    Task UpdateHdAsync(int id, BomM29Hd hd);
    Task DeleteDetailsByHdIdAsync(int hdId);
    Task InsertDetailAsync(BomM29Dt dt);
    Task<bool> DeleteAsync(int id);
}
