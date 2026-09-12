namespace AllAroundEstimates.Models;

public class EstimateData
{
    public string EstimateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public decimal SquareFootage { get; set; }
    public decimal PaverPricePerSqFt { get; set; }
    public decimal BaseMaterialCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal HourlyLaborRate { get; set; }
    public decimal ExtraCosts { get; set; }

    public decimal MaterialTotal => (SquareFootage * PaverPricePerSqFt) + BaseMaterialCost;
    public decimal LaborTotal => LaborHours * HourlyLaborRate;
    public decimal Subtotal => MaterialTotal + LaborTotal + ExtraCosts;
    public decimal MarginAmount => MaterialTotal * 0.15m;
    public decimal GrandTotal => Subtotal + MarginAmount;
}
