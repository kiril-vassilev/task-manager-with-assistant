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
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly InMemoryCollection<int, VectorStoreTasks> _collection;

    public TaskSearchPlugin(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, InMemoryCollection<int, VectorStoreTasks> collection)
    {
        _embeddingGenerator = embeddingGenerator;
        _collection = collection;
    }

    [KernelFunction, Description(
    "Search through tasks description for specific task or tasks. " +
    "Use <filterCompleted> to filter by completion status. " +
    "0 - no filter, 1 - only not completed, 2 - only completed. " +
    "Use <filterByDueDate> to filter by due date. " +
    "0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future. " +
    " Do not use it for finding task by title or giving all tasks.")]
    public async Task<IEnumerable<TaskItem>> SearchAsync(string query, int filterCompleted = 0, int filterByDueDate = 0)
    {
        var searchVector = (await _embeddingGenerator.GenerateAsync(query)).Vector;

        // Define filterIsCompleted predicate based on filterCompleted
        Expression<Func<VectorStoreTasks, bool>>? filterIsCompleted = filterCompleted switch
        {
            1 => t => t.IsCompleted == false,
            2 => t => t.IsCompleted == true,
            _ => null
        };

        // Define filterByDueDate predicate based on filterByDueDate
        Expression<Func<VectorStoreTasks, bool>>? filterDueDate = filterByDueDate switch
        {
            1 => t =>  t.DueDate < DateTime.UtcNow.Date,
            2 => t =>  t.DueDate == DateTime.UtcNow.Date,
            3 => t =>  t.DueDate > DateTime.UtcNow.Date,
            _ => null
        };

        // Combine filters if both are present
        Expression<Func<VectorStoreTasks, bool>>? combinedFilter = null;
        if (filterIsCompleted != null && filterDueDate != null)
        {
            var param = Expression.Parameter(typeof(VectorStoreTasks), "t");
            var body = Expression.AndAlso(
                Expression.Invoke(filterIsCompleted, param),
                Expression.Invoke(filterDueDate, param)
            );
            combinedFilter = Expression.Lambda<Func<VectorStoreTasks, bool>>(body, param);
        }
        else if (filterIsCompleted != null)
            combinedFilter = filterIsCompleted;
        else if (filterDueDate != null)
            combinedFilter = filterDueDate;

        // Use combinedFilter in the search
        var resultRecords = combinedFilter is null
            ? await _collection.SearchAsync(searchVector, top: 100).ToListAsync()
            : await _collection.SearchAsync(searchVector, top: 100, new() { Filter = combinedFilter }).ToListAsync();

        if (resultRecords.Count == 0)
        {
            return Enumerable.Empty<TaskItem>();
        }

        var tasks = new List<TaskItem>();
        foreach (var record in resultRecords.Where(r => r.Score > 0.3))
        {
            var task = new TaskItem
            {
                Id = record.Record.Id,
                Title = record.Record.Title,
                DueDate = record.Record.DueDate,
                Description = record.Record.Description,
                IsCompleted = record.Record.IsCompleted
            };
            tasks.Add(task);
        }

        return tasks;
    }
}
    

