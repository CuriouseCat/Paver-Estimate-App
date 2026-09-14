using System.Text.Json;
using AllAroundEstimates.Models;

namespace AllAroundEstimates.Services;

public static class EstimateStorage
{
    private const string FileName = "estimates.json";

    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static void SaveEstimates(List<SavedEstimate> estimates)
    {
        var json = JsonSerializer.Serialize(estimates, EstimateJsonContext.Default.ListSavedEstimate);
        File.WriteAllText(FilePath, json);
    }

    public static List<SavedEstimate> LoadEstimates()
    {
        if (!File.Exists(FilePath))
            return new List<SavedEstimate>();

        var json = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<SavedEstimate>();

        return JsonSerializer.Deserialize(json, EstimateJsonContext.Default.ListSavedEstimate) ?? new List<SavedEstimate>();
    }

    public static void AddEstimate(SavedEstimate estimate)
    {
        var estimates = LoadEstimates();
        estimates.Add(estimate);
        SaveEstimates(estimates);
    }

    /// <summary>
    /// Adds a new estimate record, or replaces the existing one with the same EstimateNumber if
    /// this estimate has already been saved before (e.g. re-saving after loading it for edits, or
    /// auto-saving on every PDF export). Change orders are untouched by this -- they always append
    /// via AddEstimate, since several can legitimately share the same original EstimateNumber.
    /// </summary>
    public static void SaveOrUpdateEstimate(SavedEstimate estimate)
    {
        var estimates = LoadEstimates();
        var index = estimates.FindIndex(e => !e.IsChangeOrder && e.EstimateNumber == estimate.EstimateNumber);
        if (index >= 0)
        {
            estimates[index] = estimate;
        }
        else
        {
            estimates.Add(estimate);
        }

        SaveEstimates(estimates);
    }

    /// <summary>
    /// Original estimates only (excludes change orders), newest first, for the change order
    /// page's "pull up a customer's previous estimate" lookup.
    /// </summary>
    public static List<SavedEstimate> GetPreviousEstimates()
    {
        return LoadEstimates()
            .Where(e => !e.IsChangeOrder)
            .OrderBy(e => e.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(e => e.DateCreated)
            .ToList();
    }

    /// <summary>Distinct customer names with at least one saved estimate, alphabetical.</summary>
    public static List<string> GetDistinctCustomerNames()
    {
        return LoadEstimates()
            .Where(e => !e.IsChangeOrder)
            .Select(e => e.CustomerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>A specific customer's estimates, newest first.</summary>
    public static List<SavedEstimate> GetEstimatesForCustomer(string customerName)
    {
        return LoadEstimates()
            .Where(e => !e.IsChangeOrder && string.Equals(e.CustomerName, customerName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.DateCreated)
            .ToList();
    }
}
