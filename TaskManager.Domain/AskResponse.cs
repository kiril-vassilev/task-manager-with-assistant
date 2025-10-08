namespace TaskManager.Domain;

public class AskResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
