using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

public class SavedEstimate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DateCreated { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = string.Empty;
    public string EstimateNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsChangeOrder { get; set; }

    /// <summary>Full input data for this estimate (null for change-order records), so it can be
    /// reloaded into New Estimate for editing later. The summary fields above stay lightweight for
    /// list/lookup display; this is the whole picture.</summary>
    public EstimateData? EstimateDetail { get; set; }

    [JsonIgnore]
    public string DisplayText => $"{CustomerName} — {EstimateNumber} — {TotalAmount:C2}";
}
