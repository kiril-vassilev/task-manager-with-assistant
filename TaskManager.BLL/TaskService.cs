using TaskManager.DAL;
using TaskManager.Domain;

namespace TaskManager.BLL;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repo;
    public TaskService(ITaskRepository repo) => _repo = repo;

    public IEnumerable<TaskItem> GetTasks() => _repo.GetAll();

    public TaskItem Create(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Title))
            throw new ArgumentException("Title cannot be empty");
        if (task.DueDate.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Due date cannot be in the past");
        return _repo.Add(task);
    }

    public void MarkComplete(int id)
    {
        var task = _repo.GetById(id) ?? throw new KeyNotFoundException("Task not found");
        task.IsCompleted = true;
        _repo.Update(task);
    }

        public void Delete(int id)
    {
        var task = _repo.GetById(id) ?? throw new KeyNotFoundException("Task not found");
        _repo.Delete(task.Id);
    }

    public TaskItem? FindByTitle(string title)
    {
        return _repo.FindByTitle(title);
    }
}
