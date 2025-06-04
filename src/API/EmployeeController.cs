using API;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly DeviceContext _db;

    public EmployeeController(DeviceContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await _db.Employees
            .Include(e => e.Person)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FullName = $"{e.Person.FirstName} {e.Person.LastName}"
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployeeDetails(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.Person)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound("Employee not found.");

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

        return Ok(dto);
    }
}