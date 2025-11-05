using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using TaskManager.Domain;
using System.Linq.Expressions;


namespace TaskManager.BLL.Search;


public class TaskSearchService
{
    private IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private InMemoryCollection<int, VectorStoreTasks>? _collection;

    public TaskSearchService()
    {
        // Empty constructor for DI
    }
    public async Task InitializeAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
        // Construct an InMemory vector store.
        var vectorStore = new InMemoryVectorStore();

        // Get and create collection if it doesn't exist.
        _collection = vectorStore.GetCollection<int, VectorStoreTasks>("sktasks");
        await _collection.EnsureCollectionExistsAsync();
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
        if (_embeddingGenerator == null)
            throw new InvalidOperationException("_embeddingGenerator not initialized.");

        if (_collection == null)
            throw new InvalidOperationException("_collection not initialized.");


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
        if (_collection == null)
            throw new InvalidOperationException("_collection not initialized.");

        await _collection.DeleteAsync(task.Id);
    }

    // Search through tasks description for specific task or tasks. 
    // Use <filterCompleted> to filter by completion status. 
    // 0 - no filter, 1 - only not completed, 2 - only completed.
    // Use <filterByDueDate> to filter by due date. 
    // 0 - no filter, 1 - only past due, 2 - only due today, 3 - only due in future.
    public async Task<IEnumerable<TaskItem>> SearchAsync(string query, int filterCompleted = 0, int filterByDueDate = 0)
    {
        if (_embeddingGenerator == null)
            throw new InvalidOperationException("_embeddingGenerator not initialized.");

        if (_collection == null)
            throw new InvalidOperationException("_collection not initialized.");


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
