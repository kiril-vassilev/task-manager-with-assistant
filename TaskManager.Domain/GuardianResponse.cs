using System.Text.Json.Serialization;

namespace TaskManager.Domain;

public class GuardianResponse
{
    [JsonPropertyName("Answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("IsThreatDetected")]
    public bool IsThreatDetected { get; set; }
}
