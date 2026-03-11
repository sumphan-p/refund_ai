using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using imp_api.DTOs;
using imp_api.Services;

namespace imp_api.Controllers;

[Authorize]
[ApiController]
[Route("api/import-excel")]
public class ImportExcelController : ControllerBase
{
    private readonly IImportExcelService _service;

    public ImportExcelController(IImportExcelService service)
    {
        _service = service;
    }

    /// <summary>
    /// Upload Excel file and return preview data (does NOT save to DB)
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ErrorResponse { Error = "NO_FILE", Message = "กรุณาเลือกไฟล์ Excel" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xls" && ext != ".xlsx")
            return BadRequest(new ErrorResponse { Error = "INVALID_FILE", Message = "รองรับเฉพาะไฟล์ .xls และ .xlsx เท่านั้น" });

        try
        {
            using var stream = file.OpenReadStream();
            var records = await _service.ParseExcelAsync(stream);

            if (records.Count == 0)
                return BadRequest(new ErrorResponse { Error = "EMPTY_FILE", Message = "ไม่พบข้อมูลในไฟล์ Excel" });

            var preview = await _service.PreviewAsync(records);

            return Ok(new
            {
                totalRows = records.Count,
                newRows = preview.Count(p => !p.IsExisting),
                updateRows = preview.Count(p => p.IsExisting),
                data = preview
            });
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "PARSE_ERROR",
                Message = $"ไม่สามารถอ่านไฟล์ Excel ได้: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Save uploaded Excel data to database (upsert by DeclarNo + ItemDeclarNo)
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ErrorResponse { Error = "NO_FILE", Message = "กรุณาเลือกไฟล์ Excel" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xls" && ext != ".xlsx")
            return BadRequest(new ErrorResponse { Error = "INVALID_FILE", Message = "รองรับเฉพาะไฟล์ .xls และ .xlsx เท่านั้น" });

        try
        {
            using var stream = file.OpenReadStream();
            var records = await _service.ParseExcelAsync(stream);

            var userName = User.Identity?.Name ?? "system";
            var result = await _service.SaveAsync(records, userName);

            return Ok(result);
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }
}
