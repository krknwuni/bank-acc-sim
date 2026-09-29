using System.Security.Claims;
using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankAccSim.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public class AccountController(IAccountService accounts) : ControllerBase
{
    private int CustomerId => int.TryParse(User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedException("Invalid token.");

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try { return Ok(await accounts.GetBalanceAsync(CustomerId)); }
        catch (AppException ex) { return Fail(ex); }
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(MoneyRequest request)
    {
        try { return Ok(await accounts.DepositAsync(CustomerId, request)); }
        catch (AppException ex) { return Fail(ex); }
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(MoneyRequest request)
    {
        try { return Ok(await accounts.WithdrawAsync(CustomerId, request)); }
        catch (AppException ex) { return Fail(ex); }
    }

    private ObjectResult Fail(AppException ex) => Problem(statusCode: ex.StatusCode, title: ex.Title, detail: ex.Message);
}