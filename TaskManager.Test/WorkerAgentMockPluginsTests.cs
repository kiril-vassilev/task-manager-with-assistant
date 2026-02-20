using System.Linq;
using System.Threading.Tasks;
using TaskManager.Test.Fixtures;
using TaskManager.Domain;
using Xunit;

namespace TaskManager.Test
{
    public class WorkerAgentMockPluginsTests
    {
        [Fact]
        public async Task TodayTool_ReturnsDateString()
        {
            var result = await toolsPlugin.Today();
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        [Fact]
        public async Task InMemoryTaskServicePlugin_CRUD_Works()
        {
            var svc = new InMemoryTaskServicePlugin();

            var all = (await svc.GetTasksAsync()).ToList();
            Assert.True(all.Count >= 2);

            var newTask = new TaskItem { Title = "New Task", Description = "Created in test", DueDate = System.DateTime.Now, IsCompleted = false };
            var created = await svc.CreateAsync(newTask);
            Assert.Equal("New Task", created.Title);

            var found = await svc.FindByNameAsync("New Task");
            Assert.NotNull(found);

            var marked = await svc.MarkCompleteAsync("New Task");
            Assert.NotNull(marked);
            Assert.True(marked.IsCompleted);

            var deleted = await svc.DeleteAsync("New Task");
            Assert.True(deleted);
        }

        [Fact]
        public async Task InMemoryTaskSearchPlugin_FindsByQuery()
        {
            var svc = new InMemoryTaskServicePlugin();
            var search = new InMemoryTaskSearchPlugin(svc);

            var results = (await search.SearchAsync("Sample")).ToList();
            Assert.True(results.Count >= 1);
        }
    }
}
