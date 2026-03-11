using imp_api.Models;

namespace imp_api.Repositories;

public interface IBomBoiRepository
{
    Task<IEnumerable<BomBoiHd>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize);
    Task<int> CountAsync(string? formulaNo, string? description, string? productType);
    Task<BomBoiHd?> GetByIdAsync(int id);
    Task<BomBoiHd?> GetByFormulaNoAsync(string formulaNo);
    Task<IEnumerable<BomBoiDt>> GetDetailsByHdIdAsync(int hdId);
    Task<int> InsertHdAsync(BomBoiHd hd);
    Task UpdateHdAsync(int id, BomBoiHd hd);
    Task DeleteDetailsByHdIdAsync(int hdId);
    Task InsertDetailAsync(BomBoiDt dt);
    Task<bool> DeleteAsync(int id);
}
