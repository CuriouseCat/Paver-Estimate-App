namespace AllAroundEstimates.Models;

public class SavedEstimate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DateCreated { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = string.Empty;
    public string EstimateNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsChangeOrder { get; set; }
}
