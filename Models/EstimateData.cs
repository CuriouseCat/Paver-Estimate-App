namespace AllAroundEstimates.Models;

public class EstimateData
{
    public const decimal SalesTaxRate = 0.06m;
    public const decimal EmployeeTaxRate = 0.027m;
    public const decimal CorporateTaxRate = 0.055m;

    public string EstimateNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
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

    /// <summary>Pre-tax contract total. Kept as the historical meaning of "Grand Total" so saved
    /// estimates and change-order lookups referencing it stay consistent.</summary>
    public decimal GrandTotal => Subtotal + MarginAmount;

    /// <summary>Charged to the customer and shown on their PDF.</summary>
    public decimal SalesTaxAmount => GrandTotal * SalesTaxRate;

    /// <summary>Charged to the customer and shown on their PDF, per owner instruction.</summary>
    public decimal EmployeeTaxAmount => GrandTotal * EmployeeTaxRate;

    /// <summary>Sales Tax + Employee Tax combined -- the customer-facing PDF shows these as one
    /// "Tax" line rather than itemizing them separately.</summary>
    public decimal CombinedTaxAmount => SalesTaxAmount + EmployeeTaxAmount;

    /// <summary>Owner's own business tax obligation on this job -- internal reference only, never
    /// billed to the customer or shown on their PDF.</summary>
    public decimal CorporateTaxAmount => GrandTotal * CorporateTaxRate;

    /// <summary>The actual amount owed by the customer, including sales and employee tax.</summary>
    public decimal TotalDue => GrandTotal + SalesTaxAmount + EmployeeTaxAmount;
}
