using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Controllers;


[ApiController]
[Route("api/positions")]
public class PositionsController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(DeviceContext db, ILogger<PositionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPositions()
    {
        try
        {
            var positions = await _db.Positions
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions");
            return StatusCode(500, "Internal error");
        }
    }
}