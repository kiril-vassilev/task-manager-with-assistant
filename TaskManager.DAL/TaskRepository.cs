using TaskManager.Domain;

namespace TaskManager.DAL;

public class TaskRepository : ITaskRepository
{
    private readonly TaskDbContext _context;
    public TaskRepository(TaskDbContext context) => _context = context;


    // Use <filterCompleted> to filter by completion status. 
    // 0 - no filter, 1 - only not completed, 2 - only completed.
    // Use <filterByDueDate> to filter by due date. 
    // 0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future.
    public IEnumerable<TaskItem> GetAll(int filterCompleted = 0, int filterByDueDate = 0)
    {
        var queryCompleted = _context.Tasks.AsQueryable();

        if (filterCompleted == 1)
            queryCompleted = queryCompleted.Where(t => !t.IsCompleted);
        else if (filterCompleted == 2)
            queryCompleted = queryCompleted.Where(t => t.IsCompleted);


        var queryDueDate = _context.Tasks.AsQueryable();

        
        if (filterByDueDate == 1)
            queryDueDate = queryDueDate.Where(t => t.DueDate < DateTime.Now.Date);
        else if (filterByDueDate == 2)
            queryDueDate = queryDueDate.Where(t => t.DueDate == DateTime.Now.Date);
        else if (filterByDueDate == 3)
            queryDueDate = queryDueDate.Where(t => t.DueDate > DateTime.Now.Date);


        var query = queryCompleted.Intersect(queryDueDate);

        return query.OrderBy(t => t.DueDate).ToList();
    }

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
