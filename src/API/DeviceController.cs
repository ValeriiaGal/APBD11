using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Controllers;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly DeviceContext _db;

    public DeviceController(DeviceContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await _db.Devices
            .Select(d => new DeviceDto { Id = d.Id, Name = d.Name })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetDevice(int id)
    {
        var device = await _db.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceEmployees)
                .ThenInclude(de => de.Employee)
                    .ThenInclude(e => e.Person)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound("Device not found.");
        
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDevice([FromBody] CreateUpdateDeviceDto dto)
    {
        var deviceType = await _db.DeviceTypes.FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);
        if (deviceType == null)
            return BadRequest("Invalid device type.");

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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] CreateUpdateDeviceDto dto)
    {
        var device = await _db.Devices
            .Include(d => d.DeviceEmployees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound("Device not found.");

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
            return BadRequest("Invalid device type.");

        device.Name = dto.Name;
        device.IsEnabled = dto.IsEnabled;
        device.DeviceTypeId = deviceType.Id;
        device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var device = await _db.Devices
            .Include(d => d.DeviceEmployees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound("Device not found.");

        if (device.DeviceEmployees.Any())
            return BadRequest("Cannot delete device. It is currently assigned to employees.");

        _db.Devices.Remove(device);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
