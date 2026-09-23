namespace BankAccSim.Models;

public enum TransactionType 
{
    Deposit,
    Withdrawal
}
public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public TransactionType Type { get; set; }

    
    public int AccountId  { get; set; }
    public Account Account { get; set; } = null!;
}