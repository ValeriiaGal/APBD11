using API;
using DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuthController> _logger;

    public AuthController(DeviceContext db, ITokenService tokenService, IPasswordHasher<Account> passwordHasher, ILogger<AuthController> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Login attempt for user: {Username}", dto.Username);

        try
        {
            var account = await _db.Accounts.Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (account == null)
            {
                _logger.LogWarning("Login failed: user not found: {Username}", dto.Username);
                return Unauthorized("Invalid credentials.");
            }

            var result = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, dto.Password);
            if (result != PasswordVerificationResult.Success)
            {
                _logger.LogWarning("Login failed: invalid password for user: {Username}", dto.Username);
                return Unauthorized("Invalid credentials.");
            }

            var token = _tokenService.GenerateToken(account);
            _logger.LogInformation("Login successful for user: {Username}", dto.Username);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user: {Username}", dto.Username);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
