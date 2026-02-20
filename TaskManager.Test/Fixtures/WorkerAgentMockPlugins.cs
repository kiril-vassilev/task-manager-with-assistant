using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using TaskManager.Domain;

#nullable enable

namespace TaskManager.Test.Fixtures
{
    // Lightweight mock implementations of the tools/plugins the worker may call.
    public static class toolsPlugin
    {
        public static Boolean IsTodayCalled { get; private set; } = false;
        public static Boolean IsClearCalled { get; private set; } = false;
        public static void ResetIsTodayCalled() => IsTodayCalled = false;
        public static void ResetIsClearCalled() => IsClearCalled = false;
        public static Task<string> Today()
        {
            IsTodayCalled = true;
            return Task.FromResult(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        public static Task<string> Clear()
        {
            IsClearCalled = true;
            // Represents clearing a temporary state; returns OK.
            return Task.FromResult("Cleared");
        }
    }

    public class InMemoryTaskServicePlugin
    {
        private readonly List<TaskItem> _store = new();

        public InMemoryTaskServicePlugin()
        {
            // seed with a couple of items
            _store.Add(new TaskItem { Title = "Sample Task 1", Description = "This is a sample task", DueDate = DateTime.Now.AddDays(1), IsCompleted = false });
            _store.Add(new TaskItem { Title = "Sample Task 2", Description = "Another sample task", DueDate = DateTime.Now.AddDays(2), IsCompleted = false });
        }

        public Task<IEnumerable<TaskItem>> GetTasksAsync()
        {
            return Task.FromResult(_store.AsEnumerable());
        }

        public Task<TaskItem> CreateAsync(TaskItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _store.Add(item);
            return Task.FromResult(item);
        }

        public Task<TaskItem?> FindByNameAsync(string title)
        {
            var found = _store.FirstOrDefault(t => string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(found);
        }

        public Task<TaskItem?> MarkCompleteAsync(string title)
        {
            var item = _store.FirstOrDefault(t => string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase));
            if (item == null) return Task.FromResult<TaskItem?>(null);
            item.IsCompleted = true;
            return Task.FromResult<TaskItem?>(item);
        }

        public Task<bool> DeleteAsync(string title)
        {
            var item = _store.FirstOrDefault(t => string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase));
            if (item == null) return Task.FromResult(false);
            _store.Remove(item);
            return Task.FromResult(true);
        }
    }

    public class InMemoryTaskSearchPlugin
    {
        private readonly InMemoryTaskServicePlugin _taskService;

        public InMemoryTaskSearchPlugin(InMemoryTaskServicePlugin taskService)
        {
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        }

        public async Task<IEnumerable<TaskItem>> SearchAsync(string query)
        {
            var tasks = await _taskService.GetTasksAsync();
            if (string.IsNullOrWhiteSpace(query)) return tasks;
            var q = query.Trim();
            return tasks.Where(t => (t.Title != null && t.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) || (t.Description != null && t.Description.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
