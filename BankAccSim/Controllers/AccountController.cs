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
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class AccountController(IAccountService accounts) : ControllerBase
{
    private int CustomerId => int.TryParse(User.FindFirstValue("sub"), out var id)
        ? id
        : throw new UnauthorizedException("Invalid token.");

    // "200" -> Current balance of the user's account
    // "404" -> The user has no account
    [HttpGet("balance")]
    [ProducesResponseType<BalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance()
        => Ok(await accounts.GetBalanceAsync(CustomerId));

    // "200" -> Deposit recorded; returns the new balance
    // "400" -> Invalid amount
    [HttpPost("deposit")]
    [ProducesResponseType<OperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(MoneyRequest request)
        => Ok(await accounts.DepositAsync(CustomerId, request));

    // "200" -> Withdrawal recorded; returns the new balance
    // "400" -> Invalid amount
    // "422" -> Insufficient funds
    [HttpPost("withdraw")]
    [ProducesResponseType<OperationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(MoneyRequest request)
        => Ok(await accounts.WithdrawAsync(CustomerId, request));
}