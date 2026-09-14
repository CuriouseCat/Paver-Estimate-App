using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

[JsonSerializable(typeof(List<ServiceType>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ServiceTypeJsonContext : JsonSerializerContext
{
}
