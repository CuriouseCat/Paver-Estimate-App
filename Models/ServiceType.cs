namespace AllAroundEstimates.Models;

public class ServiceType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal PricePerSqFt { get; set; }
    public decimal HourlyLaborRate { get; set; }
}
