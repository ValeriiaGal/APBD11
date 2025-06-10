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
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(DeviceContext db, ILogger<EmployeeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllEmployees()
    {
        _logger.LogInformation("Fetching all employees...");
        try
        {
            var employees = await _db.Employees
                .Include(e => e.Person)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    FullName = $"{e.Person.FirstName} {e.Person.LastName}"
                })
                .ToListAsync();

            _logger.LogInformation("Successfully fetched {Count} employees.", employees.Count);
            return Ok(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching employees.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmployeeDetails(int id)
    {
        _logger.LogInformation("Fetching employee details for ID: {Id}", id);
        try
        {
            var employee = await _db.Employees
                .Include(e => e.Person)
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {Id} not found.", id);
                return NotFound("Employee not found.");
            }

            var dto = new EmployeeDetailsDto
            {
                PassportNumber = employee.Person.PassportNumber,
                FirstName = employee.Person.FirstName,
                MiddleName = employee.Person.MiddleName,
                LastName = employee.Person.LastName,
                PhoneNumber = employee.Person.PhoneNumber,
                Email = employee.Person.Email,
                Salary = employee.Salary,
                HireDate = employee.HireDate,
                Position = new PositionDto
                {
                    Id = employee.Position.Id,
                    Name = employee.Position.Name
                }
            };

            _logger.LogInformation("Successfully fetched details for employee ID {Id}.", id);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching employee ID {Id}.", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
