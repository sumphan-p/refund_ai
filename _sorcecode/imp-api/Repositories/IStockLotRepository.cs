using imp_api.Models;

namespace imp_api.Repositories;

public interface IStockLotRepository
{
    Task<IEnumerable<StockLot>> GetActiveLotsFifoAsync(string rawMaterialCode, string privilegeType, DateTime exportDate);
    Task<IEnumerable<StockLot>> GetAllActiveLotsFifoAsync(string rawMaterialCode, string privilegeType);
    Task<StockLot?> GetByIdAsync(int id);
    Task<StockLot?> GetByImportDeclarAsync(string importDeclarNo, int importItemNo);
    Task<int> InsertAsync(StockLot lot);
    Task UpdateQtyAsync(int id, decimal qtyUsed, decimal qtyBalance, string status);
    Task<IEnumerable<StockLot>> SearchAsync(string? importDeclarNo, string? rawMaterialCode, string? privilegeType, string? status, int page, int pageSize);
    Task<int> CountAsync(string? importDeclarNo, string? rawMaterialCode, string? privilegeType, string? status);
}
