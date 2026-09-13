using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

[JsonSerializable(typeof(List<SavedEstimate>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class EstimateJsonContext : JsonSerializerContext
{
}
