using TaskManager.Domain;

namespace TaskManager.DAL;

public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;
    public TaskRepository(TaskDbContext context) => _context = context;

    public IEnumerable<TaskItem> GetAll() => _context.Tasks.OrderBy(t => t.DueDate).ToList();

    public TaskItem? GetById(int id) => _context.Tasks.Find(id);

    public TaskItem Add(TaskItem task)
    {
        _context.Tasks.Add(task);
        _context.SaveChanges();
        return task;
    }

    public void Update(TaskItem task)
    {
        _context.Tasks.Update(task);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var entity = _context.Tasks.Find(id);
        if (entity is null) return;
        _context.Tasks.Remove(entity);
        _context.SaveChanges();
    }

    public TaskItem? FindByTitle(string title)
    {
        return _context.Tasks.FirstOrDefault(t => t.Title == title);
    }
}
