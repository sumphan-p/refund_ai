using imp_api.DTOs;
using imp_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace imp_api.Controllers;

[ApiController]
[Route("api/formula-m29")]
[Authorize]
public class FormulaM29Controller : ControllerBase
{
    private readonly IFormulaM29Service _service;

    public FormulaM29Controller(IFormulaM29Service service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? formulaNo,
        [FromQuery] string? description,
        [FromQuery] string? productType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _service.SearchAsync(formulaNo, description, productType, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return NotFound(new ErrorResponse { Error = ex.Error, Message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormulaM29Request request)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            var id = await _service.CreateAsync(request, userName);
            return Ok(new { id });
        }
        catch (AppException ex)
        {
            return ex.Error switch
            {
                "DUPLICATE" => Conflict(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                "VALIDATION_ERROR" => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
                _ => BadRequest(new ErrorResponse { Error = ex.Error, Message = ex.Message }),
            };
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFormulaM29Request request)
    {
        var userName = User.Identity?.Name ?? "system";
        try
        {
            await _service.UpdateAsync(id, request, userName);
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
