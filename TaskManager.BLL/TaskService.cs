using TaskManager.DAL;
using TaskManager.Domain;
using TaskManager.BLL.Search;

namespace TaskManager.BLL;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repo;
    private readonly TaskSearchService _taskSearchService;

    public TaskService(ITaskRepository repo, TaskSearchService taskSearchService)
    {
        _repo = repo;
        _taskSearchService = taskSearchService;
    }
    
    // Use <filterCompleted> to filter by completion status. 
    // 0 - no filter, 1 - only not completed, 2 - only completed.
    // Use <filterByDueDate> to filter by due date. 
    // 0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future.
    public IEnumerable<TaskItem> GetTasks(int filterCompleted = 0, int filterByDueDate = 0)
    {
        return _repo.GetAll(filterCompleted, filterByDueDate);
    }

    public TaskItem Create(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Title))
            throw new ArgumentException("Title cannot be empty");

        if (string.IsNullOrWhiteSpace(task.Description))
            throw new ArgumentException("Description cannot be empty");

        if (task.DueDate.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Due date cannot be in the past");

        // Add the task to vector store
        _taskSearchService.AddVectorStoreTaskEntry(task).Wait();

        return _repo.Add(task);
    }

    public void MarkComplete(int id)
    {
        var task = _repo.GetById(id) ?? throw new KeyNotFoundException("Task not found");
        task.IsCompleted = true;

        // Update the task in vector store
        _taskSearchService.AddVectorStoreTaskEntry(task).Wait();

        _repo.Update(task);
    }

        public void Delete(int id)
    {
        var task = _repo.GetById(id) ?? throw new KeyNotFoundException("Task not found");

        // Remove the task from vector store
        _taskSearchService.RemoveVectorStoreTaskEntry(task).Wait();

        _repo.Delete(task.Id);
    }

    public TaskItem? FindByTitle(string title)
    {
        return _repo.FindByTitle(title);
    }
}
