using imp_api.DTOs;
using imp_api.Models;

namespace imp_api.Services;

public interface IExportExcelService
{
    Task<List<ExportExcel>> ParseExcelAsync(Stream fileStream);
    Task<List<ExportExcelPreviewItem>> PreviewAsync(List<ExportExcel> records);
    Task<ExportExcelUploadResponse> SaveAsync(List<ExportExcel> records, string userName);
}
