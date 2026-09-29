using BankAccSim.Data;
using BankAccSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankAccSim.Tests;

internal static class TestHelpers
{
    public static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static async Task<int> SeedCustomerAsync(AppDbContext db, string email = "test@example.com")
    {
        var customer = new Customer
        {
            Name = "Test",
            Email = email,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            Accounts =
            {
                new Account { AccountNumber = "BS" + Guid.NewGuid().ToString("N")[..16], Currency = "USD", CreatedAt = DateTime.UtcNow }
            }
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }
}
