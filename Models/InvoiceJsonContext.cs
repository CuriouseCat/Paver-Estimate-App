using System.Text.Json.Serialization;

namespace AllAroundEstimates.Models;

[JsonSerializable(typeof(List<Invoice>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class InvoiceJsonContext : JsonSerializerContext
{
}
