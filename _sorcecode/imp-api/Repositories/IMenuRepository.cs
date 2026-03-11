using imp_api.Models;

namespace imp_api.Repositories;

public interface IMenuRepository
{
    Task<IEnumerable<Menu>> GetAllAsync();
    Task<Menu?> GetByIdAsync(int id);
    Task<Menu?> GetByMenuCodeAsync(string menuCode);
    Task<Menu> CreateAsync(Menu menu);
    Task UpdateAsync(Menu menu);
}
