using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace Controllers;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(DeviceContext db, ILogger<DeviceController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDevices()
    {
        try
        {
            _logger.LogInformation("Fetching all devices");
            var devices = await _db.Devices
                .Select(d => new DeviceDto { Id = d.Id, Name = d.Name })
                .ToListAsync();
            return Ok(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching devices");
            return StatusCode(500, "Internal error");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetDevice(int id)
    {
        try
        {
            _logger.LogInformation("Fetching device with ID {DeviceId}", id);
            var device = await _db.Devices
                .Include(d => d.DeviceType)
                .Include(d => d.DeviceEmployees)
                    .ThenInclude(de => de.Employee)
                        .ThenInclude(e => e.Person)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                _logger.LogWarning("Device with ID {DeviceId} not found", id);
                return NotFound("Device not found.");
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "User")
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var account = await _db.Accounts.FindAsync(userId);
                if (account == null || !device.DeviceEmployees.Any(de => de.EmployeeId == account.EmployeeId && de.ReturnDate == null))
                    return Forbid();
            }

            var employee = device.DeviceEmployees
                .Where(de => de.ReturnDate == null)
                .OrderByDescending(de => de.IssueDate)
                .Select(de => new EmployeeDto
                {
                    Id = de.Employee.Id,
                    FullName = $"{de.Employee.Person.FirstName} {de.Employee.Person.LastName}"
                })
                .FirstOrDefault();

            var details = new DeviceInfoDto
            {
                Name = device.Name,
                DeviceTypeName = device.DeviceType?.Name ?? "Unknown",
                IsEnabled = device.IsEnabled,
                AdditionalProperties = JsonSerializer.Deserialize<Dictionary<string, object>>(device.AdditionalProperties) ?? new(),
                CurrentEmployee = employee
            };

            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching device with ID {DeviceId}", id);
            return StatusCode(500, "Internal error");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDevice([FromBody] CreateUpdateDeviceDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new device");
            var deviceType = await _db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
            if (deviceType == null)
            {
                _logger.LogWarning("Invalid device type: {DeviceType}", dto.DeviceTypeName);
                return BadRequest("Invalid device type.");
            }

            var device = new Device
            {
                Name = dto.Name,
                IsEnabled = dto.IsEnabled,
                DeviceTypeId = deviceType.Id,
                AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties)
            };

            _db.Devices.Add(device);
            await _db.SaveChangesAsync();

            return Created($"/api/device/{device.Id}", new { device.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return StatusCode(500, "Internal error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] CreateUpdateDeviceDto dto)
    {
        try
        {
            _logger.LogInformation("Updating device with ID {DeviceId}", id);
            var device = await _db.Devices
                .Include(d => d.DeviceEmployees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                _logger.LogWarning("Device with ID {DeviceId} not found", id);
                return NotFound("Device not found.");
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "User")
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var account = await _db.Accounts.FindAsync(userId);
                if (account == null || !device.DeviceEmployees.Any(de => de.EmployeeId == account.EmployeeId && de.ReturnDate == null))
                    return Forbid();
            }

            var deviceType = await _db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
            if (deviceType == null)
            {
                _logger.LogWarning("Invalid device type: {DeviceType}", dto.DeviceTypeName);
                return BadRequest("Invalid device type.");
            }

            device.Name = dto.Name;
            device.IsEnabled = dto.IsEnabled;
            device.DeviceTypeId = deviceType.Id;
            device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);

            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device with ID {DeviceId}", id);
            return StatusCode(500, "Internal error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        try
        {
            _logger.LogInformation("Deleting device with ID {DeviceId}", id);
            var device = await _db.Devices
                .Include(d => d.DeviceEmployees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (device == null)
            {
                _logger.LogWarning("Device with ID {DeviceId} not found", id);
                return NotFound("Device not found.");
            }

            if (device.DeviceEmployees.Any())
            {
                _logger.LogWarning("Device with ID {DeviceId} is assigned to employees", id);
                return BadRequest("Cannot delete device. It is currently assigned to employees.");
            }

            _db.Devices.Remove(device);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device with ID {DeviceId}", id);
            return StatusCode(500, "Internal error");
        }
    }
    
    
    [HttpGet("types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        try
        {
            _logger.LogInformation("Fetching all device types");
            var types = await _db.DeviceTypes
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching device types");
            return StatusCode(500, "Internal error");
        }
    }

}
