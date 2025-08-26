using TaskManager.Domain;

namespace TaskManager.BLL;

public interface ITaskService
{
    IEnumerable<TaskItem> GetTasks();
    TaskItem Create(TaskItem task);
    void MarkComplete(int id);
    TaskItem? FindByName(string name);
}
