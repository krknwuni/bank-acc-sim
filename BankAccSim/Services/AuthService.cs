using System.Security.Cryptography;
using BankAccSim.Data;
using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BankAccSim.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

public class AuthService(AppDbContext db, ITokenService tokens) : IAuthService
{
    private const string DefaultCurrency = "USD";

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Customers.AnyAsync(c => c.Email == email, ct))
            throw new ConflictException("A user with this e-mail already exists.");

        var now = DateTime.UtcNow;
        var account = new Account
        {
            AccountNumber = GenerateAccountNumber(),
            Currency = DefaultCurrency,
            CreatedAt = now
        };
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = now,
            Accounts = { account }
        };

        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("A user with this e-mail already exists.");
        }

        return new RegisterResponse(customer.Id, customer.Name, customer.Email, account.AccountNumber);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);

        if (customer is null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
            throw new UnauthorizedException("Invalid e-mail or password.");

        var (token, expiresAt) = tokens.CreateToken(customer);
        return new AuthResponse(token, expiresAt);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateAccountNumber() =>
        "BS" + string.Concat(Enumerable.Range(0, 16).Select(_ => RandomNumberGenerator.GetInt32(10)));
}