using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApiRefreshTokenDemo.DTOs;
using WebApiRefreshTokenDemo.Models;

namespace WebApiRefreshTokenDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpPost("create")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (await _roleManager.RoleExistsAsync(dto.RoleName))
        {
            return BadRequest($"Role '{dto.RoleName}' already exists");
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(dto.RoleName));

        if (result.Succeeded is false)
        {
            return BadRequest(result.Errors);
        }

        return Ok($"Role '{dto.RoleName}' created successfully");
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return BadRequest($"User with email '{dto.Email}' not found.");
        }

        var alreadyInRole = await _userManager.IsInRoleAsync(user, dto.RoleName);

        if (alreadyInRole is true)
        {
            return Ok($"Role '{dto.RoleName}' already assigned to user '{user.Email}'.");
        }

        if ((await _roleManager.RoleExistsAsync(dto.RoleName)) is false)
        {
            return BadRequest($"The role {dto.RoleName} doesn't exists.");
        }

        var result = await _userManager.AddToRoleAsync(user, dto.RoleName);

        if (result.Succeeded is false)
        {
            return BadRequest(result.Errors);
        }

        return Ok($"Role '{dto.RoleName}' assigned to user '{user.Email}' successfully.");
    }
}