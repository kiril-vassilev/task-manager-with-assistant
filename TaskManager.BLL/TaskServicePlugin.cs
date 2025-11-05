using System.ComponentModel;
using TaskManager.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.BLL;

public sealed class TaskServicePlugin
{
    private readonly IServiceProvider _serviceProvider;

    public TaskServicePlugin(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    [Description("Provides a list of tasks." +
        "Use <filterCompleted> to filter by completion status. " +
        "0 - no filter, 1 - only not completed, 2 - only completed. " +
        "Use <filterByDueDate> to filter by due date. " +
        "0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future. " +
        "Do not use it for searching by description or finding a task by name.")]
    public Task<IEnumerable<TaskItem>> GetTasksAsync(int filterCompleted = 0, int filterByDueDate = 0)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.GetTasks(filterCompleted, filterByDueDate));
    }

    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.Create(task));
    }

    public Task<TaskItem?> FindByNameAsync(string title)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.FindByTitle(title));
    }

    public Task<TaskItem?> MarkCompleteAsync(string title)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var task = taskService.FindByTitle(title);

        if (task is null)
            return Task.FromResult<TaskItem?>(null);

        taskService.MarkComplete(task.Id);

        return Task.FromResult<TaskItem?>(task);
    }

    public Task<string> DeleteAsync(string title)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var task = taskService.FindByTitle(title);

        if (task is null)
            return Task.FromResult("Task not found.");

        taskService.Delete(task.Id);
        return Task.FromResult("Task deleted successfully.");
    }
}
