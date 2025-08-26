using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Domain;

namespace TaskManager.BLL;

public sealed class TaskServicePlugin
{
    private readonly ITaskService _taskService;

    public TaskServicePlugin(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [KernelFunction, Description("Provides a list of tasks.")]
    public Task<IEnumerable<TaskItem>> GetTasksAsync()
    {
        // Wrap synchronous call in Task.FromResult
        return Task.FromResult(_taskService.GetTasks());
    }

    [KernelFunction, Description("Creates a new task.")]
    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        return Task.FromResult(_taskService.Create(task));
    }

    [KernelFunction, Description("Finds a task by name.")]
    public Task<TaskItem?> FindByNameAsync(string name)
    {
        return Task.FromResult(_taskService.FindByName(name));
    }

    [KernelFunction, Description("Marks a task as complete.")]
    public Task MarkCompleteAsync(int id)
    {
        _taskService.MarkComplete(id);
        return Task.CompletedTask;
    }
}
