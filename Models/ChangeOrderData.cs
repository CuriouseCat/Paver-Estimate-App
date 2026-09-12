namespace AllAroundEstimates.Models;

public class ChangeOrderData
{
    public string OriginalEstimateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public decimal OriginalTotalAmount { get; set; }
    public string DescriptionOfChanges { get; set; } = string.Empty;
    public decimal CostOfChange { get; set; }

    public decimal RevisedTotal => OriginalTotalAmount + CostOfChange;
}
