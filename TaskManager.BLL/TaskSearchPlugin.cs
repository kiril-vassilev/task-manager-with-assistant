using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using System.Linq.Expressions;
using System.Linq;

namespace TaskManager.BLL;

public sealed class TaskSearchPlugin
{
    private readonly TaskSearchService _taskSearchService;

    public TaskSearchPlugin(TaskSearchService taskSearchService)
    {
        _taskSearchService = taskSearchService;
    }

    [KernelFunction, Description(
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
    

