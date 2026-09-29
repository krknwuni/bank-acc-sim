using System.Security.Claims;
using BankAccSim.Data;
using BankAccSim.Dtos;
using BankAccSim.Models;
using BankAccSim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankAccSim.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Customers.AnyAsync(c => c.Email == email))
            return Problem(statusCode: 409, title: "Conflict", detail: "A user with this e-mail already exists.");

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return StatusCode(201, new RegisterResponse(customer.Id, customer.Name, customer.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == email);

        if (customer is null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
            return Problem(statusCode: 401, title: "Unauthorized", detail: "Invalid e-mail or password.");

        var (token, expiresAt) = tokens.CreateToken(customer);
        return Ok(new AuthResponse(token, expiresAt));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = User.FindFirstValue("sub"),
        Email = User.FindFirstValue("email")
    });
}