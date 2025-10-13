using TaskManager.Domain;

namespace TaskManager.DAL;

public interface ITaskRepository
{
    IEnumerable<TaskItem> GetAll(int filterCompleted = 0, int filterByDueDate = 0);
    TaskItem? GetById(int id);
    TaskItem Add(TaskItem task);
    void Update(TaskItem task);
    void Delete(int id);
    TaskItem? FindByTitle(string title);
}
