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
}
