using TaskManager.Domain;

namespace TaskManager.BLL;

public interface ITaskService
{
    IEnumerable<TaskItem> GetTasks(int filterCompleted = 0, int filterByDueDate = 0);
    TaskItem Create(TaskItem task);
    void MarkComplete(int id);
    void Delete(int id);
    TaskItem? FindByTitle(string title);
}
