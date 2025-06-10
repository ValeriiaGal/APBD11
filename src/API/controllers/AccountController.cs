using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tokens;

namespace Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly IPasswordHasher<Account> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(DeviceContext db, IPasswordHasher<Account> passwordHasher, ILogger<AccountController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    private bool IsValidUsername(string username) => !string.IsNullOrWhiteSpace(username) && !char.IsDigit(username[0]);

    private bool IsValidPassword(string password)
    {
        return password.Length >= 12
               && Regex.IsMatch(password, "[a-z]")
               && Regex.IsMatch(password, "[A-Z]")
               && Regex.IsMatch(password, "[0-9]")
               && Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            _logger.LogInformation("Starting account registration");

            if (!IsValidUsername(dto.Username))
                return BadRequest("Username cannot start with a digit.");
            if (!IsValidPassword(dto.Password))
                return BadRequest("Password must be at least 12 characters and include uppercase, lowercase, number, and symbol.");
            if (await _db.Accounts.AnyAsync(a => a.Username == dto.Username))
                return BadRequest("Username already exists.");

            var employee = await _db.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return BadRequest("Invalid employee ID.");

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.Role);
            if (role == null)
                return BadRequest("Invalid role.");

            var account = new Account
            {
                Username = dto.Username,
                PasswordHash = _passwordHasher.HashPassword(null, dto.Password),
                EmployeeId = dto.EmployeeId,
                RoleId = role.Id
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Account registered successfully with ID {Id}", account.Id);
            return Created($"/api/account/{account.Id}", new { account.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account registration");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        try
        {
            _logger.LogInformation("Fetching all accounts");
            var accounts = await _db.Accounts
                .Select(a => new { a.Id, a.Username, a.PasswordHash })
                .ToListAsync();
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching accounts");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccount(int id)
    {
        try
        {
            _logger.LogInformation("Fetching account with ID {Id}", id);
            var acc = await _db.Accounts.FindAsync(id);
            if (acc == null) return NotFound();
            return Ok(new { acc.Username, acc.PasswordHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching account with ID {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] RegisterDto dto)
    {
        try
        {
            _logger.LogInformation("Updating account with ID {Id}", id);
            var acc = await _db.Accounts.FindAsync(id);
            if (acc == null) return NotFound();

            acc.Username = dto.Username;
            acc.PasswordHash = _passwordHasher.HashPassword(null, dto.Password);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account with ID {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            _logger.LogInformation("Deleting account with ID {Id}", id);
            var acc = await _db.Accounts.FindAsync(id);
            if (acc == null) return NotFound();

            _db.Accounts.Remove(acc);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account with ID {Id}", id);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
