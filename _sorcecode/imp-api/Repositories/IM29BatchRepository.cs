using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Repositories;

public interface IM29BatchRepository
{
    Task<int> InsertHeaderAsync(M29BatchHeader header);
    Task InsertItemAsync(M29BatchItem item);
    Task<M29BatchHeader?> GetHeaderByIdAsync(int id);
    Task<M29BatchHeader?> GetHeaderByDocNoAsync(string batchDocNo);
    Task<IEnumerable<M29BatchItem>> GetItemsByHeaderIdAsync(int headerId);
    Task<(IEnumerable<BatchListItem> Items, int TotalCount)> SearchAsync(
        string? batchDocNo, string? status, string? dateFrom, string? dateTo, int page, int pageSize);
    Task UpdateStatusAsync(int id, string status, string? confirmedBy);
    Task CancelAsync(int id, string? cancelledBy);
    Task<int> GetMaxRunningNoAsync(string buddhistYearSuffix);
    Task<M29BatchHeader?> GetLatestHeaderByYearAsync(string buddhistYearSuffix);
}
