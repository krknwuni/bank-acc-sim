using System.ComponentModel.DataAnnotations;

namespace BankAccSim.Dtos;

public class MoneyRequest
{
    [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000.")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }
}

public record BalanceResponse(string AccountNumber, string Currency, decimal Balance);

public record OperationResponse(int TransactionId, string Type, decimal Amount, decimal NewBalance, DateTime CreatedAt);