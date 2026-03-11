using imp_api.Models;

namespace imp_api.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role> CreateAsync(Role role);
    Task UpdateAsync(Role role);
    Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId);
    Task<Dictionary<Guid, List<string>>> GetRoleNamesByUserIdsAsync(IEnumerable<Guid> userIds);
    Task SetUserRolesAsync(Guid userId, IEnumerable<int> roleIds);
}
