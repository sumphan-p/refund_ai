using imp_api.DTOs;

namespace imp_api.Services;

public interface IPrivilege19TvisService
{
    Task<PagedResponse<ExportItemForCutting>> SearchExportsAsync(string? declarNo, string? productCode, string? dateFrom, string? dateTo, int page, int pageSize);
    Task<CutStock19TvisResponse> CalculateFifoAsync(CutStock19TvisRequest request, string userName);
    Task<ExportCuttingDetail> GetCuttingDetailAsync(string exportDeclarNo, int exportItemNo);
    Task ConfirmCuttingAsync(string exportDeclarNo, int exportItemNo, string userName);
    Task CancelCuttingAsync(string exportDeclarNo, int exportItemNo, string userName);
    Task<SyncStockLotResponse> SyncImportToStockLotAsync(string userName);
    Task<PagedResponse<StockLotListItem>> SearchLotsAsync(string? importDeclarNo, string? rawMaterialCode, string? status, int page, int pageSize);
}
