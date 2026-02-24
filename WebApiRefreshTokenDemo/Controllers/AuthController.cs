using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApiRefreshTokenDemo.DTOs;
using WebApiRefreshTokenDemo.Models;
using WebApiRefreshTokenDemo.Options;
using WebApiRefreshTokenDemo.Services;

namespace WebApiRefreshTokenDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IOptions<JwtSettingsOptions> jwtSettingsOptions) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ITokenService _tokenService = tokenService;
    private readonly JwtSettingsOptions _jwtSettingsOptions = jwtSettingsOptions.Value;


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

        SetTokensInCookies(new TokenResponseDto(accessToken, refreshToken.Token));

        return Ok();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out string? refreshToken) is false)
        {
            return BadRequest(new { Message = "No refresh token passed" });
        }

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

        SetTokensInCookies(new TokenResponseDto(newAccessToken, newRefreshToken.Token));

        return Ok();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out string? token) is false)
        {
            return BadRequest(new { Message = "No refresh token passed" });
        }

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

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");

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

    private void SetTokensInCookies(TokenResponseDto dto)
    {
        Response.Cookies.Append(
            "accessToken",
            dto.AccessToken,
            new()
            {
                MaxAge = TimeSpan.FromMinutes(_jwtSettingsOptions.AccessTokenExpirationMinutes),
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Secure = true,
            });

        Response.Cookies.Append(
            "refreshToken",
            dto.RefreshToken,
            new()
            {
                MaxAge = TimeSpan.FromDays(_jwtSettingsOptions.RefreshTokenExpirationDays),
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Secure = true
            });
    }
}
