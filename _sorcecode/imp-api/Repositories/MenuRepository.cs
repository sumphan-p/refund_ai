using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly IDbConnection _db;

    public MenuRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Menu>> GetAllAsync()
    {
        return await _db.QueryAsync<Menu>(
            @"SELECT Id, ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive, CreatedAt
              FROM imp.Menu
              ORDER BY SortOrder");
    }

    public async Task<Menu?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<Menu>(
            "SELECT Id, ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive, CreatedAt FROM imp.Menu WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Menu?> GetByMenuCodeAsync(string menuCode)
    {
        return await _db.QuerySingleOrDefaultAsync<Menu>(
            "SELECT Id, ParentId, MenuCode, MenuName, Icon, Route, SortOrder, IsActive, CreatedAt FROM imp.Menu WHERE MenuCode = @MenuCode",
            new { MenuCode = menuCode });
    }

    public async Task<Menu> CreateAsync(Menu menu)
    {
        menu.Id = await _db.QuerySingleAsync<int>(
            @"INSERT INTO imp.Menu (ParentId, MenuCode, MenuName, Icon, Route, SortOrder)
              OUTPUT INSERTED.Id
              VALUES (@ParentId, @MenuCode, @MenuName, @Icon, @Route, @SortOrder)",
            new { menu.ParentId, menu.MenuCode, menu.MenuName, menu.Icon, menu.Route, menu.SortOrder });
        return menu;
    }

    public async Task UpdateAsync(Menu menu)
    {
        await _db.ExecuteAsync(
            @"UPDATE imp.Menu
              SET ParentId = @ParentId, MenuCode = @MenuCode, MenuName = @MenuName,
                  Icon = @Icon, Route = @Route, SortOrder = @SortOrder, IsActive = @IsActive
              WHERE Id = @Id",
            new { menu.Id, menu.ParentId, menu.MenuCode, menu.MenuName, menu.Icon, menu.Route, menu.SortOrder, menu.IsActive });
    }
}
