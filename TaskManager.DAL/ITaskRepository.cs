using TaskManager.Domain;

namespace TaskManager.DAL;

public interface ITaskRepository
{
    IEnumerable<TaskItem> GetAll();
    TaskItem? GetById(int id);
    TaskItem Add(TaskItem task);
    void Update(TaskItem task);
    void Delete(int id);
    TaskItem? FindByName(string name);
}
