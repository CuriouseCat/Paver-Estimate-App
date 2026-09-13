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
}
