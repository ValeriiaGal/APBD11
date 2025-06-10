using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tokens;

namespace Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly DeviceContext _db;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<Account> _passwordHasher;

    public AuthController(DeviceContext db, ITokenService tokenService, IPasswordHasher<Account> passwordHasher)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var account = await _db.Accounts.Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (account == null)
                return Unauthorized("Invalid credentials.");

            var result = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
                return Unauthorized("Invalid credentials.");

            var token = _tokenService.GenerateToken(account);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}