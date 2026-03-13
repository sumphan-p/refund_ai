using imp_api.DTOs;
using imp_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace imp_api.Controllers;

[ApiController]
[Route("api/privilege-19tvis")]
[Authorize]
public class Privilege19TvisController : ControllerBase
{
    private readonly IPrivilege19TvisService _service;

    public Privilege19TvisController(IPrivilege19TvisService service)
    {
        _service = service;
    }

    /// <summary>Search export items eligible for Section 19 bis cutting</summary>
    [HttpGet("exports")]
    public async Task<IActionResult> SearchExports(
        [FromQuery] string? declarNo,
        [FromQuery] string? productCode,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchExportsAsync(declarNo, productCode, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    /// <summary>Calculate FIFO cutting for an export item</summary>
    [HttpPost("cut")]
    public async Task<IActionResult> CalculateFifo([FromBody] CutStock19TvisRequest request)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            var result = await _service.CalculateFifoAsync(request, userName);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return ex.Error switch
            {
                "ALREADY_CUT" => Conflict(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                "FORMULA_NOT_FOUND" or "FORMULA_EMPTY" => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                _ => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
            };
        }
    }

    /// <summary>Get cutting detail for an export item</summary>
    [HttpGet("cutting-detail")]
    public async Task<IActionResult> GetCuttingDetail(
        [FromQuery] string exportDeclarNo,
        [FromQuery] int exportItemNo)
    {
        try
        {
            var result = await _service.GetCuttingDetailAsync(exportDeclarNo, exportItemNo);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    /// <summary>Confirm cutting (PENDING → CONFIRMED)</summary>
    [HttpPut("confirm")]
    public async Task<IActionResult> ConfirmCutting(
        [FromQuery] string exportDeclarNo,
        [FromQuery] int exportItemNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.ConfirmCuttingAsync(exportDeclarNo, exportItemNo, userName);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error switch
            {
                "NOT_FOUND" => NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                _ => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
            };
        }
    }

    /// <summary>Cancel cutting (restore lot qty)</summary>
    [HttpPut("cancel")]
    public async Task<IActionResult> CancelCutting(
        [FromQuery] string exportDeclarNo,
        [FromQuery] int exportItemNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.CancelCuttingAsync(exportDeclarNo, exportItemNo, userName);
            return NoContent();
        }
        catch (AppException ex)
        {
            return ex.Error switch
            {
                "NOT_FOUND" => NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                _ => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
            };
        }
    }

    /// <summary>Sync import_excel → stock_m29_lot (create lots from imports)</summary>
    [HttpPost("sync-lots")]
    public async Task<IActionResult> SyncLots()
    {
        var userName = User.Identity?.Name ?? "system";
        var result = await _service.SyncImportToStockLotAsync(userName);
        return Ok(result);
    }

    /// <summary>Get export lines by exact DeclarNo</summary>
    [HttpGet("export-lines")]
    public async Task<IActionResult> GetExportLines([FromQuery] string declarNo)
    {
        if (string.IsNullOrWhiteSpace(declarNo))
            return BadRequest(new ErrorResponse { Error = "VALIDATION_ERROR", Message = "กรุณากรอกเลขที่ใบขนขาออก" });

        var result = await _service.GetExportLinesByDeclarNoAsync(declarNo);
        return Ok(result);
    }

    /// <summary>Get BOM formula with calculated quantities</summary>
    [HttpGet("bom-formula")]
    public async Task<IActionResult> GetBomFormula([FromQuery] string formulaNo, [FromQuery] decimal exportQty = 0)
    {
        try
        {
            var result = await _service.GetBomFormulaAsync(formulaNo, exportQty);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    /// <summary>Get available import lots for a material (FIFO order)</summary>
    [HttpGet("available-lots")]
    public async Task<IActionResult> GetAvailableLots([FromQuery] string rawMaterialCode)
    {
        var result = await _service.GetAvailableLotsForMaterialAsync(rawMaterialCode);
        return Ok(result);
    }

    /// <summary>Get stock card entries for a material</summary>
    [HttpGet("stock-card")]
    public async Task<IActionResult> GetStockCard([FromQuery] string rawMaterialCode)
    {
        var result = await _service.GetStockCardByMaterialAsync(rawMaterialCode);
        return Ok(result);
    }

    /// <summary>Search stock lots</summary>
    [HttpGet("lots")]
    public async Task<IActionResult> SearchLots(
        [FromQuery] string? importDeclarNo,
        [FromQuery] string? rawMaterialCode,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchLotsAsync(importDeclarNo, rawMaterialCode, status, page, pageSize);
        return Ok(result);
    }
}
