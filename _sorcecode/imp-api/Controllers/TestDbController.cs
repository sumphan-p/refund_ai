using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using imp_api.DTOs;

namespace imp_api.Controllers;

[ApiController]
[Route("api/testdb")]
public class TestDbController : ControllerBase
{
    private readonly IDbConnection _db;

    public TestDbController(IDbConnection db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var result = await _db.QuerySingleAsync<DateTime>("SELECT GETDATE()");
            return Ok(new
            {
                Status = "Connected",
                ServerTime = result,
                Database = _db.Database
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Error = "DB_CONNECTION_FAILED",
                Message = ex.Message
            });
        }
    }
}
