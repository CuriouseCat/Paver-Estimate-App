namespace AllAroundEstimates.Models;

public enum InvoiceType
{
    Deposit,
    Final
}

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EstimateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.Now;
}
