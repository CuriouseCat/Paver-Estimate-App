using System.Text.Json;
using AllAroundEstimates.Models;

namespace AllAroundEstimates.Services;

public static class ServiceTypeStorage
{
    private const string FileName = "servicetypes.json";

    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static List<ServiceType> LoadServiceTypes()
    {
        if (!File.Exists(FilePath))
        {
            var defaults = new List<ServiceType>
            {
                new() { Name = "Installation" },
                new() { Name = "Repair" }
            };
            SaveServiceTypes(defaults);
            return defaults;
        }

        var json = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<ServiceType>();

        return JsonSerializer.Deserialize(json, ServiceTypeJsonContext.Default.ListServiceType) ?? new List<ServiceType>();
    }

    public static void SaveServiceTypes(List<ServiceType> serviceTypes)
    {
        var json = JsonSerializer.Serialize(serviceTypes, ServiceTypeJsonContext.Default.ListServiceType);
        File.WriteAllText(FilePath, json);
    }
}
