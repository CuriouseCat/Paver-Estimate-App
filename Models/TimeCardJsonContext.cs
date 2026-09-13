using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

[JsonSerializable(typeof(List<TimeCard>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class TimeCardJsonContext : JsonSerializerContext
{
}
