using System.Text.Json.Serialization;

namespace TaskManager.Domain;

public enum RedirectType
{
    None,
    QnAAgent,
    WorkerAgent
}

public class FirstLineResponse
{
    [JsonPropertyName("Answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("Redirect")]
    public RedirectType Redirect { get; set; } = RedirectType.None;
}
