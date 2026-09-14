namespace AllAroundEstimates.Models;

public class ServiceLineItem
{
    public string ServiceTypeName { get; set; } = string.Empty;
    public decimal SquareFootage { get; set; }
    public decimal PricePerSqFt { get; set; }

    public decimal Total => SquareFootage * PricePerSqFt;
}
