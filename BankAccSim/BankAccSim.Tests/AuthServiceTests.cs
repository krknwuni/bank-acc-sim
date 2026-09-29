using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Models;
using BankAccSim.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BankAccSim.Tests;

public class AuthServiceTests
{
    private static Mock<ITokenService> TokenServiceMock()
    {
        var mock = new Mock<ITokenService>();
        mock.Setup(t => t.CreateToken(It.IsAny<Customer>()))
            .Returns(("fake-token", DateTime.UtcNow.AddHours(1)));
        return mock;
    }

    private static RegisterRequest NewUser(string email = "ann@example.com") =>
        new() { Name = "Ann", Email = email, Password = "Secret123!" };

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        await using var db = TestHelpers.CreateDb();
        var service = new AuthService(db, TokenServiceMock().Object);

        await service.RegisterAsync(NewUser("  ANN@Example.com "));

        var customer = await db.Customers.SingleAsync();
        Assert.NotEqual("Secret123!", customer.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secret123!", customer.PasswordHash));
        Assert.Equal("ann@example.com", customer.Email); // trimmed + lower-cased
    }

    [Fact]
    public async Task Register_CreatesAccount()
    {
        await using var db = TestHelpers.CreateDb();
        var service = new AuthService(db, TokenServiceMock().Object);

        var result = await service.RegisterAsync(NewUser());

        Assert.Equal(1, await db.Accounts.CountAsync());
        Assert.StartsWith("BS", result.AccountNumber);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws_Conflict()
    {
        await using var db = TestHelpers.CreateDb();
        var service = new AuthService(db, TokenServiceMock().Object);
        await service.RegisterAsync(NewUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(NewUser("ANN@example.com")));
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        await using var db = TestHelpers.CreateDb();
        var tokens = TokenServiceMock();
        var service = new AuthService(db, tokens.Object);
        await service.RegisterAsync(NewUser());

        var result = await service.LoginAsync(new LoginRequest { Email = "ann@example.com", Password = "Secret123!" });

        Assert.Equal("fake-token", result.Token);
        tokens.Verify(t => t.CreateToken(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws_Unauthorized_AndIssuesNoToken()
    {
        await using var db = TestHelpers.CreateDb();
        var tokens = TokenServiceMock();
        var service = new AuthService(db, tokens.Object);
        await service.RegisterAsync(NewUser());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync(new LoginRequest { Email = "ann@example.com", Password = "wrong-password" }));

        tokens.Verify(t => t.CreateToken(It.IsAny<Customer>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnknownEmail_Throws_Unauthorized()
    {
        await using var db = TestHelpers.CreateDb();
        var service = new AuthService(db, TokenServiceMock().Object);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "whatever123" }));
    }
}
