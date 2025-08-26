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
        var tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Title = "Title1",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(7),
                IsCompleted = false
            },
            new TaskItem
            {
                Title = "Title2",
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(7),
                IsCompleted = false
            }
        };

        return Task.FromResult<IEnumerable<TaskItem>>(tasks);
        // return Task.FromResult(_taskService.GetTasks());
    }

    [KernelFunction, Description("Creates a new task.")]
    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        return Task.FromResult(task);
        // return Task.FromResult(_taskService.Create(task));
    }

    [KernelFunction, Description("Finds a task by title.")]
    public Task<TaskItem?> FindByNameAsync(string title)
    {
        var task = new TaskItem
        {
            Title = title,
            Description = "Description",
            DueDate = DateTime.UtcNow.AddDays(7),
            IsCompleted = false
        };

        return Task.FromResult(task);
        // return Task.FromResult(_taskService.FindByName(title));
    }

    [KernelFunction, Description("Marks a task as complete.")]
    public Task MarkCompleteAsync(int id)
    {
        // _taskService.MarkComplete(id);
        return Task.CompletedTask;
    }
}
