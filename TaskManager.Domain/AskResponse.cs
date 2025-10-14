using System.Text.Json.Serialization;

namespace TaskManager.Domain;

public class AskResponse
{
    [JsonPropertyName("Answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("Tasks")]
    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
