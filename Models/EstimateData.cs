namespace AllAroundEstimates.Models;

public class EstimateData
{
    public string EstimateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public decimal SquareFootage { get; set; }
    public decimal PaverPricePerSqFt { get; set; }
    public decimal BaseMaterialCost { get; set; }
    public decimal LaborHours { get; set; }
    public decimal HourlyLaborRate { get; set; }
    public decimal NumberOfEmployees { get; set; }
    public decimal ExtraCosts { get; set; }
    public List<CustomCharge> CustomCharges { get; set; } = new();

    public decimal MaterialTotal => (SquareFootage * PaverPricePerSqFt) + BaseMaterialCost;
    public decimal TotalLaborHours => LaborHours * NumberOfEmployees;
    public decimal LaborTotal => TotalLaborHours * HourlyLaborRate;
    public decimal CustomChargesTotal => CustomCharges.Sum(c => c.Amount);
    public decimal Subtotal => MaterialTotal + LaborTotal + ExtraCosts + CustomChargesTotal;
    public decimal MarginAmount => MaterialTotal * 0.20m;
    public decimal GrandTotal => Subtotal + MarginAmount;
}
