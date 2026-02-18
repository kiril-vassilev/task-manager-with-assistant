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
using System.ComponentModel;

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

            var guardianAgent = CreateAgent(client, _agentConfiguration.GetGuardianAgentInstructions(), "GuardianAgent");
            var guardianAgentExecutor = new GuardianAgentExecutor(guardianAgent);

            var firstLineAgent = CreateAgent(client, _agentConfiguration.GetFirstLineAgentInstructions(), "FirstLineAgent");
            var firstLineAgentExecutor = new FirstLineAgentExecutor(firstLineAgent);

            var qnaAgent = CreateAgent(client, _agentConfiguration.GetQnAAgentInstructions(), "QnAAgent" );
            var qnaAgentExecutor = new QnAAgentExecutor(qnaAgent);
            
            var workerAgent = await CreateWorkerAgentAsync(client, scope);
            var workerAgentExecutor = new WorkerAgentExecutor(workerAgent);

            await _agentService.InitializeAsync(guardianAgentExecutor, firstLineAgentExecutor, qnaAgentExecutor, workerAgentExecutor);

        }
        await Task.CompletedTask;
    }

    private ChatClientAgent CreateAgent(OpenAI.Chat.ChatClient client, string instructions, string name)
    {
        ChatClientAgent agent = client.AsAIAgent(
            instructions: instructions,
            name: name);

        return agent;
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
                    AIFunctionFactory.Create(toolsPlugin.Today, name: "Today", description: "It returns today's date."),
                    AIFunctionFactory.Create(toolsPlugin.ClearAsync, name: "Clear", description: "It clears the chat history and the context."),
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