using System.ComponentModel;
using TaskManager.Domain;

namespace TaskManager.BLL.Search;

public sealed class TaskSearchPlugin
{
    private readonly TaskSearchService _taskSearchService;

    public TaskSearchPlugin(TaskSearchService taskSearchService)
    {
        _taskSearchService = taskSearchService;
    }

    [Description(
    "Search through tasks description for specific task or tasks. " +
    "Use <filterCompleted> to filter by completion status. " +
    "0 - no filter, 1 - only not completed, 2 - only completed. " +
    "Use <filterByDueDate> to filter by due date. " +
    "0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future. " +
    "Do not use it for finding task by title or giving all tasks. " )]
    public async Task<IEnumerable<TaskItem>> SearchAsync(string query, int filterCompleted = 0, int filterByDueDate = 0)
    {
        return await _taskSearchService.SearchAsync(query, filterCompleted, filterByDueDate);
    }
}
    

