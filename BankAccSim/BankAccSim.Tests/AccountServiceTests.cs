using BankAccSim.Dtos;
using BankAccSim.Exceptions;
using BankAccSim.Services;
using Microsoft.EntityFrameworkCore;

namespace BankAccSim.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task NewAccount_HasZeroBalance()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);

        var balance = await service.GetBalanceAsync(id);

        Assert.Equal(0m, balance.Balance);
    }

    [Fact]
    public async Task Deposit_IncreasesBalance()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);

        var result = await service.DepositAsync(id, new MoneyRequest { Amount = 100m });

        Assert.Equal(100m, result.NewBalance);
        Assert.Equal(100m, (await service.GetBalanceAsync(id)).Balance);
    }

    [Fact]
    public async Task Withdraw_DecreasesBalance()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);
        await service.DepositAsync(id, new MoneyRequest { Amount = 100m });

        var result = await service.WithdrawAsync(id, new MoneyRequest { Amount = 40.50m });

        Assert.Equal(59.50m, result.NewBalance);
    }

    [Fact]
    public async Task Withdraw_MoreThanBalance_Throws_AndBalanceIsUnchanged()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);
        await service.DepositAsync(id, new MoneyRequest { Amount = 50m });

        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => service.WithdrawAsync(id, new MoneyRequest { Amount = 50.01m }));

        Assert.Equal(50m, (await service.GetBalanceAsync(id)).Balance);
        Assert.Equal(1, await db.Transactions.CountAsync()); // the failed withdrawal was not recorded
    }

    [Fact]
    public async Task Withdraw_ExactBalance_IsAllowed()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);
        await service.DepositAsync(id, new MoneyRequest { Amount = 50m });

        var result = await service.WithdrawAsync(id, new MoneyRequest { Amount = 50m });

        Assert.Equal(0m, result.NewBalance);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(0.001)] // more than 2 decimal places
    public async Task Deposit_InvalidAmount_Throws(double amount)
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);

        await Assert.ThrowsAsync<BadRequestException>(
            () => service.DepositAsync(id, new MoneyRequest { Amount = (decimal)amount }));
    }

    [Fact]
    public async Task Balance_IsSumOfAllTransactions()
    {
        await using var db = TestHelpers.CreateDb();
        var id = await TestHelpers.SeedCustomerAsync(db);
        var service = new AccountService(db);

        await service.DepositAsync(id, new MoneyRequest { Amount = 100m });
        await service.WithdrawAsync(id, new MoneyRequest { Amount = 30.50m });
        await service.DepositAsync(id, new MoneyRequest { Amount = 10.25m });

        Assert.Equal(79.75m, (await service.GetBalanceAsync(id)).Balance);
    }

    [Fact]
    public async Task CustomersCannotSeeEachOthersMoney()
    {
        await using var db = TestHelpers.CreateDb();
        var alice = await TestHelpers.SeedCustomerAsync(db, "alice@example.com");
        var bob = await TestHelpers.SeedCustomerAsync(db, "bob@example.com");
        var service = new AccountService(db);

        await service.DepositAsync(alice, new MoneyRequest { Amount = 100m });

        Assert.Equal(0m, (await service.GetBalanceAsync(bob)).Balance);
        await Assert.ThrowsAsync<InsufficientFundsException>(
            () => service.WithdrawAsync(bob, new MoneyRequest { Amount = 1m }));
    }

    [Fact]
    public async Task UnknownCustomer_Throws_NotFound()
    {
        await using var db = TestHelpers.CreateDb();
        var service = new AccountService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetBalanceAsync(12345));
    }
}
