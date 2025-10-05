using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Domain;
using System.Linq.Expressions;
using System.Linq;


namespace TaskManager.BLL;


public class TaskSearchService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly InMemoryCollection<int, VectorStoreTasks> _collection;

    public TaskSearchService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
        _collection = CreateTextSearchCollectionAsync().Result;
    }

    private async Task<InMemoryCollection<int, VectorStoreTasks>> CreateTextSearchCollectionAsync()
    {
        // Construct an InMemory vector store.
        var vectorStore = new InMemoryVectorStore();

        // Get and create collection if it doesn't exist.
        var collection = vectorStore.GetCollection<int, VectorStoreTasks>("sktasks");
        await collection.EnsureCollectionExistsAsync();

        return collection;
    }

    public async Task AddVectorStoreTasksEntries(IEnumerable<TaskItem> tasks)
    {
        var tasksEntries = tasks
            .Where(task => task.Description != null && task.Description != string.Empty)
            .Select(task => AddVectorStoreTaskEntry(task));

        await Task.WhenAll(tasksEntries);
    }

    public async Task AddVectorStoreTaskEntry(TaskItem task)
    {
        if (task.Description == null || task.Description == string.Empty)
            return;

        var entry = new VectorStoreTasks
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted
        };

        entry.DescriptionEmbedding = (await _embeddingGenerator.GenerateAsync(entry.Description)).Vector;

        await _collection.UpsertAsync([entry]);
    }

    public async Task RemoveVectorStoreTaskEntry(TaskItem task)
    {
        await _collection.DeleteAsync(task.Id);
    }

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
            1 => t => t.DueDate < DateTime.UtcNow.Date,
            2 => t => t.DueDate == DateTime.UtcNow.Date,
            3 => t => t.DueDate > DateTime.UtcNow.Date,
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
