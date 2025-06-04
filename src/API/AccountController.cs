using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly IPasswordHasher<Account> _passwordHasher;

    public AccountController(DeviceContext db, IPasswordHasher<Account> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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

    // POST: /api/account
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
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
        return Created($"/api/accounts/{account.Id}", new { account.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _db.Accounts
            .Select(a => new { a.Id, a.Username, a.PasswordHash })
            .ToListAsync();
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var acc = await _db.Accounts.FindAsync(id);
        if (acc == null) return NotFound();
        return Ok(new { acc.Username, acc.PasswordHash });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAccount(int id, [FromBody] RegisterDto dto)
    {
        var acc = await _db.Accounts.FindAsync(id);
        if (acc == null) return NotFound();

        acc.Username = dto.Username;
        acc.PasswordHash = _passwordHasher.HashPassword(null, dto.Password);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var acc = await _db.Accounts.FindAsync(id);
        if (acc == null) return NotFound();

        _db.Accounts.Remove(acc);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
