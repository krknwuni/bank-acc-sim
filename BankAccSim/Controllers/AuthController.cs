using System.Security.Claims;
using BankAccSim.Dtos;
using BankAccSim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankAccSim.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class AuthController(IAuthService auth) : ControllerBase
{
    // "201" -> Customer created
    // "400" -> Validation failed (bad e-mail, short password, ...)
    // "409" -> A user with this e-mail already exists
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
        => StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request));

    // "200" -> Token issued
    // "400" -> Validation failed
    // "401" -> Invalid e-mail or password
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
        => Ok(await auth.LoginAsync(request));

    // "200" -> Token is valid
    // "401" -> Missing, invalid or expired token
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue("sub"),
        Email = User.FindFirstValue("email")
    });
}