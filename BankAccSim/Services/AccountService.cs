using System.Data;
using BankAccSim.Data;
using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankAccSim.Services;

public interface IAccountService
{
    Task<BalanceResponse> GetBalanceAsync(int customerId, CancellationToken ct = default);
    Task<OperationResponse> DepositAsync(int customerId, MoneyRequest request, CancellationToken ct = default);
    Task<OperationResponse> WithdrawAsync(int customerId, MoneyRequest request, CancellationToken ct = default);
}

public class AccountService(AppDbContext db) : IAccountService
{
    private const decimal MaxAmount = 1_000_000_000m;

    public async Task<BalanceResponse> GetBalanceAsync(int customerId, CancellationToken ct = default)
    {
        var account = await GetAccountAsync(customerId, ct);
        var balance = await CalculateBalanceAsync(account.Id, ct);
        return new BalanceResponse(account.AccountNumber, account.Currency, balance);
    }

    public Task<OperationResponse> DepositAsync(int customerId, MoneyRequest request, CancellationToken ct = default)
        => ApplyAsync(customerId, request, TransactionType.Deposit, ct);

    public Task<OperationResponse> WithdrawAsync(int customerId, MoneyRequest request, CancellationToken ct = default)
        => ApplyAsync(customerId, request, TransactionType.Withdrawal, ct);

    private async Task<OperationResponse> ApplyAsync(
        int customerId, MoneyRequest request, TransactionType type, CancellationToken ct)
    {
        ValidateAmount(request.Amount);

        var account = await GetAccountAsync(customerId, ct);

        await using var tx = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;

        var balance = await CalculateBalanceAsync(account.Id, ct);

        if (type == TransactionType.Withdrawal && request.Amount > balance)
            throw new InsufficientFundsException(balance, request.Amount);

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Type = type,
            Amount = request.Amount,
            Description = string.IsNullOrWhiteSpace(request.Description) ? type.ToString() : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        if (tx is not null) await tx.CommitAsync(ct);

        var newBalance = type == TransactionType.Deposit ? balance + request.Amount : balance - request.Amount;
        return new OperationResponse(transaction.Id, type.ToString(), transaction.Amount, newBalance, transaction.CreatedAt);
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new BadRequestException("Amount must be greater than zero.");
        if (amount > MaxAmount)
            throw new BadRequestException($"Amount must not exceed {MaxAmount:0}.");
        if (decimal.Round(amount, 2) != amount)
            throw new BadRequestException("Amount must have at most 2 decimal places.");
    }

    private Task<decimal> CalculateBalanceAsync(int accountId, CancellationToken ct) =>
        db.Transactions
            .Where(t => t.AccountId == accountId)
            .SumAsync(t => t.Type == TransactionType.Deposit ? t.Amount : -t.Amount, ct);

    private async Task<Account> GetAccountAsync(int customerId, CancellationToken ct) =>
        await db.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customerId, ct)
        ?? throw new NotFoundException("Account not found.");
}