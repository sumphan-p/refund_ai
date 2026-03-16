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
        [FromQuery] bool uncutOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (!uncutOnly && pageSize > 100) pageSize = 100;
        if (uncutOnly && pageSize > 10000) pageSize = 10000;

        var result = await _service.SearchExportsAsync(declarNo, productCode, dateFrom, dateTo, uncutOnly, page, pageSize);
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

    /// <summary>Cancel all cuttings by BatchDocNo</summary>
    [HttpPut("cancel-batch")]
    public async Task<IActionResult> CancelBatch([FromQuery] string batchDocNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            var count = await _service.CancelByBatchDocNoAsync(batchDocNo, userName);
            return Ok(new { cancelledCount = count, batchDocNo });
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

    /// <summary>Get next cutting document number</summary>
    [HttpGet("next-doc-no")]
    public async Task<IActionResult> GetNextDocNo()
    {
        var result = await _service.GetNextDocNoAsync();
        return Ok(result);
    }

    // =============================================
    // Batch Management endpoints
    // =============================================

    /// <summary>Search batches (paginated)</summary>
    [HttpGet("batches")]
    public async Task<IActionResult> SearchBatches(
        [FromQuery] string? batchDocNo,
        [FromQuery] string? status,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchBatchesAsync(batchDocNo, status, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get batch detail</summary>
    [HttpGet("batch/{*batchDocNo}")]
    public async Task<IActionResult> GetBatchDetail(string batchDocNo)
    {
        try
        {
            var result = await _service.GetBatchDetailAsync(batchDocNo);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    /// <summary>Confirm entire batch (PENDING → CONFIRMED)</summary>
    [HttpPut("confirm-batch")]
    public async Task<IActionResult> ConfirmBatch([FromQuery] string batchDocNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.ConfirmBatchAsync(batchDocNo, userName);
            return Ok(new { batchDocNo, status = "CONFIRMED" });
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

    // =============================================
    // M29 Batch Management (m29_batch_header / m29_batch_item)
    // =============================================

    /// <summary>Search M29 batches (paginated)</summary>
    [HttpGet("m29-batches")]
    public async Task<IActionResult> SearchM29Batches(
        [FromQuery] string? batchDocNo,
        [FromQuery] string? status,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchM29BatchesAsync(batchDocNo, status, dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    /// <summary>Get next M29 batch document number</summary>
    [HttpGet("m29-next-doc-no")]
    public async Task<IActionResult> GetNextM29DocNo()
    {
        var result = await _service.GetNextM29BatchDocNoAsync();
        return Ok(result);
    }

    /// <summary>Create M29 batch (select export items)</summary>
    [HttpPost("m29-batch")]
    public async Task<IActionResult> CreateM29Batch([FromBody] CreateBatchRequest request)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            var result = await _service.CreateM29BatchAsync(request, userName);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return ex.Error switch
            {
                "BATCH_LIMIT" or "FOB_LIMIT" => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                _ => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
            };
        }
    }

    /// <summary>Get M29 batch detail</summary>
    [HttpGet("m29-batch/{*batchDocNo}")]
    public async Task<IActionResult> GetM29BatchDetail(string batchDocNo)
    {
        try
        {
            var result = await _service.GetM29BatchDetailAsync(batchDocNo);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    /// <summary>Confirm M29 batch</summary>
    [HttpPut("m29-confirm-batch")]
    public async Task<IActionResult> ConfirmM29Batch([FromQuery] string batchDocNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.ConfirmM29BatchAsync(batchDocNo, userName);
            return Ok(new { batchDocNo, status = "CONFIRMED" });
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

    /// <summary>Cancel M29 batch (DELETE header + items)</summary>
    [HttpDelete("m29-batch")]
    public async Task<IActionResult> CancelM29Batch([FromQuery] string batchDocNo)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.CancelM29BatchAsync(batchDocNo, userName);
            return Ok(new { batchDocNo, status = "CANCELLED" });
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
}
