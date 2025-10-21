using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace TaskManager.BLL;

public sealed class TaskServicePlugin
{
    private readonly IServiceProvider _serviceProvider;

    public TaskServicePlugin(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [KernelFunction, Description("Provides a list of tasks." +
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

    [KernelFunction, Description("Creates a new task. Ask the user for title, description, and due date in the future or today.")]
    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.Create(task));
    }

    [KernelFunction, Description("Finds a task by title. Do not use it for searching by description or other fields.")]
    public Task<TaskItem?> FindByNameAsync(string title)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.FindByTitle(title));
    }

    [KernelFunction, Description("Marks a task as complete.")]
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

    [KernelFunction, Description("Deletes a task. Do not check if the task exists. Make sure to confirm with the user before deleting.")]
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
