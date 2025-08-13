
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace EmulatorTests;

// test class
public record TestItem
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public required string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Name { get; init; }
    public required string PartitionKey { get; init; }
}