using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Services;

public interface IImportExcelService
{
    Task<List<ImportExcel>> ParseExcelAsync(Stream fileStream);
    Task<List<ImportExcelPreviewItem>> PreviewAsync(List<ImportExcel> records);
    Task<ImportExcelUploadResponse> SaveAsync(List<ImportExcel> records, string userName);
}
