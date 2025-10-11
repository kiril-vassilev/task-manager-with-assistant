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

    [KernelFunction, Description("Provides a list of tasks.")]
    public Task<IEnumerable<TaskItem>> GetTasksAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.FromResult(taskService.GetTasks());
    }

    [KernelFunction, Description("Creates a new task. Ask the user for title, description, and due date in the future.")]
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

    [KernelFunction, Description("Delete a task. Mkake sure to confirm with the user before deleting.")]
    public string DeleteAsync(string title)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var task = taskService.FindByTitle(title);

        if (task is null)
            return "Task not found.";

        taskService.Delete(task.Id);
        return "Task deleted successfully.";
    }
}
