using imp_api.DTOs;

namespace imp_api.Services;

public interface IPrivilege19TvisService
{
    Task<PagedResponse<ExportItemForCutting>> SearchExportsAsync(string? declarNo, string? productCode, string? dateFrom, string? dateTo, bool uncutOnly, int page, int pageSize);
    Task<CutStock19TvisResponse> CalculateFifoAsync(CutStock19TvisRequest request, string userName);
    Task<ExportCuttingDetail> GetCuttingDetailAsync(string exportDeclarNo, int exportItemNo);
    Task ConfirmCuttingAsync(string exportDeclarNo, int exportItemNo, string userName);
    Task CancelCuttingAsync(string exportDeclarNo, int exportItemNo, string userName);
    Task<SyncStockLotResponse> SyncImportToStockLotAsync(string userName);
    Task<PagedResponse<StockLotListItem>> SearchLotsAsync(string? importDeclarNo, string? rawMaterialCode, string? status, int page, int pageSize);
    Task<List<ExportLineItem>> GetExportLinesByDeclarNoAsync(string declarNo);
    Task<BomFormulaInfo> GetBomFormulaAsync(string formulaNo, decimal exportQty);
    Task<List<StockLotListItem>> GetAvailableLotsForMaterialAsync(string rawMaterialCode);
    Task<List<StockCardEntry>> GetStockCardByMaterialAsync(string rawMaterialCode);
    Task<NextDocNoResponse> GetNextDocNoAsync();
    Task<int> CancelByBatchDocNoAsync(string batchDocNo, string userName);

    // Batch management (stock_m29_batch)
    Task<PagedResponse<BatchListItem>> SearchBatchesAsync(string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize);
    Task<BatchDetailResponse> GetBatchDetailAsync(string batchDocNo);
    Task ConfirmBatchAsync(string batchDocNo, string userName);

    // M29 Batch management (m29_batch_header / m29_batch_item)
    Task<PagedResponse<BatchListItem>> SearchM29BatchesAsync(string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize);
    Task<CreateBatchResponse> CreateM29BatchAsync(CreateBatchRequest request, string userName);
    Task<NextDocNoResponse> GetNextM29BatchDocNoAsync();
    Task<M29BatchDetailResponse> GetM29BatchDetailAsync(string batchDocNo);
    Task ConfirmM29BatchAsync(string batchDocNo, string userName);
    Task CancelM29BatchAsync(string batchDocNo, string userName);
}
