namespace BankAccSim.Models;

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<Transaction> Transactions { get; set; } = [];
}