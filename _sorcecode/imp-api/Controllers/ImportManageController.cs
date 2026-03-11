using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using imp_api.DTOs;
using imp_api.Services;

namespace imp_api.Controllers;

[Authorize]
[ApiController]
[Route("api/import-manage")]
public class ImportManageController : ControllerBase
{
    private readonly IImportManageService _service;

    public ImportManageController(IImportManageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? declarNo,
        [FromQuery] string? invoiceNo,
        [FromQuery] string? productCode,
        [FromQuery] string? brand,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchAsync(declarNo, invoiceNo, productCode, brand, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var record = await _service.GetByIdAsync(id);
            return Ok(record);
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateImportManageRequest request)
    {
        try
        {
            var userName = User.Identity?.Name ?? "system";
            await _service.UpdateAsync(id, request, userName);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error == "NOT_FOUND"
                ? NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message })
                : BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }
}
