using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiRefreshTokenDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult Public() => Ok("This is a public endpoint");

    [Authorize]
    [HttpGet("protected")]
    public IActionResult Protected() => Ok("This is a protected enpoint. You are authorize");

    [Authorize(Roles = "admin")]
    [HttpGet("admin")]
    public IActionResult Admin() => Ok("This is an admin endpoint. You are authorized as an Admin.");
}

