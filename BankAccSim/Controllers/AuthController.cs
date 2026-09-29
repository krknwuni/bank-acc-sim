using System.Security.Claims;
using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankAccSim.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try { return StatusCode(201, await auth.RegisterAsync(request)); }
        catch (AppException ex) { return Fail(ex); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try { return Ok(await auth.LoginAsync(request)); }
        catch (AppException ex) { return Fail(ex); }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue("sub"),
        Email = User.FindFirstValue("email")
    });

    private ObjectResult Fail(AppException ex) => Problem(statusCode: ex.StatusCode, title: ex.Title, detail: ex.Message);
}