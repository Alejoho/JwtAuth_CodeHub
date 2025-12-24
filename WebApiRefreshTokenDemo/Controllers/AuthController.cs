using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiRefreshTokenDemo.Data;
using WebApiRefreshTokenDemo.DTOs;
using WebApiRefreshTokenDemo.Models;
using WebApiRefreshTokenDemo.Services;

namespace WebApiRefreshTokenDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ITokenService _tokenService = tokenService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded is false)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.Users
            .Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || (await _userManager.CheckPasswordAsync(user, dto.Password)) is false)
        {
            return Unauthorized(new { Message = "Invalid email or password" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var refreshToken = _tokenService.CreateRefreshToken(GetIpAddress());

        user.RefreshTokens.Add(refreshToken);

        await _userManager.UpdateAsync(user);

        return Ok(new TokenResponseDto(accessToken, refreshToken.Token));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto dto)
    {
        var refreshToken = dto.RefreshToken;

        var user = await _userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(
                t => t.Token == refreshToken));

        if (user is null)
        {
            return Unauthorized(new { Message = "Invalid refresh token" });
        }

        var existingToken = user.RefreshTokens.Single(t => t.Token == refreshToken);

        if (existingToken.IsActive is false)
        {
            return Unauthorized(new { Message = "Refresh token is inactive" });
        }

        existingToken.Revoked = DateTime.UtcNow;
        existingToken.RevokedByIp = GetIpAddress();

        var newRefreshToken = _tokenService.CreateRefreshToken(GetIpAddress());

        existingToken.ReplacedByToken = newRefreshToken.Token;
        user.RefreshTokens.Add(newRefreshToken);

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.CreateAccessToken(user, roles);

        return Ok(new TokenResponseDto(newAccessToken, newRefreshToken.Token));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] TokenRequestDto dto)
    {
        var token = dto.RefreshToken;
        var user = await _userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(
                t => t.Token == token));

        if (user is null)
        {
            return NotFound();
        }

        var existing = user.RefreshTokens.Single(t => t.Token == token);

        if (existing.IsActive is not true)
        {
            return BadRequest("The token is already revoked.");
        }

        existing.Revoked = DateTime.UtcNow;
        existing.RevokedByIp = GetIpAddress();

        await _userManager.UpdateAsync(user);

        return Ok(new { Message = "Token revoked successfully" });
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
