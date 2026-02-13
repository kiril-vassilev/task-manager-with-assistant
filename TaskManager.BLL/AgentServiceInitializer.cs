using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using TaskManager.BLL.Orchestration;
using TaskManager.BLL.Search;
using OpenAI.Chat;

namespace TaskManager.BLL;

public class AgentServiceInitializer : IHostedService
{
    private readonly AgentService _agentService;
    private readonly AgentConfiguration _agentConfiguration;
    private readonly IServiceProvider _serviceProvider;
    private readonly TaskSearchService _taskSearchService;

    public AgentServiceInitializer(
        AgentService agentService,
        AgentConfiguration agentConfiguration,  
        IServiceProvider serviceProvider,
        TaskSearchService taskSearchService)
    {
        _agentService = agentService;
        _agentConfiguration = agentConfiguration;
        _serviceProvider = serviceProvider;
        _taskSearchService = taskSearchService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var client = _agentConfiguration.CreateChatClient();

            var guardianAgent = CreateGuardianAgent(client);
            var guardianAgentExecutor = new GuardianAgentExecutor(guardianAgent);

            var workerAgent = await CreateWorkerAgentAsync(client, scope);
            var workerAgentExecutor = new WorkerAgentExecutor(workerAgent);

            _agentService.Initialize(guardianAgentExecutor, workerAgentExecutor);

        }
        await Task.CompletedTask;
    }

    private ChatClientAgent CreateGuardianAgent(OpenAI.Chat.ChatClient client)
    {
        ChatClientAgent guardianAgent = client.AsAIAgent(
            instructions: _agentConfiguration.GetGuardianAgentInstructions(),
            name: "GuardianAgent");

        return guardianAgent;
    }
    private async Task<ChatClientAgent> CreateWorkerAgentAsync(OpenAI.Chat.ChatClient client, IServiceScope scope)
    {
        var toolsPlugin = new ToolsPlugin(_agentService);

        // Create the embeding generator and initialize the TaskSearchService
        var embeddingGenerator = _agentConfiguration.CreateEmbeddingGenerator();
        await _taskSearchService.InitializeAsync(embeddingGenerator);

        // Load tasks into the vector store
        var tasks = scope.ServiceProvider.GetRequiredService<ITaskService>().GetTasks();
        await _taskSearchService.AddVectorStoreTasksEntries(tasks);

        var taskServicePlugin = new TaskServicePlugin(_serviceProvider);
        var taskSearchPlugin = new TaskSearchPlugin(_taskSearchService);


        ChatClientAgent workerAgent = client.AsAIAgent(
            instructions: _agentConfiguration.GetWorkerAgentInstructions(),
            name: "TaskManagerAgent",
            tools: [
                    AIFunctionFactory.Create(toolsPlugin.Today),
                    AIFunctionFactory.Create(toolsPlugin.Clear),
                    AIFunctionFactory.Create(taskServicePlugin.GetTasksAsync, name: "GetAllTasks"),
                    AIFunctionFactory.Create(taskServicePlugin.CreateAsync, name: "Create", description: "Creates a new task. Ask the user for title, description, and due date in the future or today."),
                    AIFunctionFactory.Create(taskServicePlugin.FindByNameAsync, name: "FindByTitle", description: "Finds a task by title. Do not use it for searching by description or other fields."),
                    AIFunctionFactory.Create(taskServicePlugin.MarkCompleteAsync, name: "MarkComplete", description: "Marks a task as complete."),
                    AIFunctionFactory.Create(taskServicePlugin.DeleteAsync, name: "Delete", description: "Deletes a task. It is IMPORTANT to confirm with the user before deleting one or more tasks."),
                    AIFunctionFactory.Create(taskSearchPlugin.SearchAsync, name: "Search")
            ]);

        return workerAgent;
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask; // kill the agent
}