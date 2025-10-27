using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;

namespace TaskManager.BLL;

public class AgentTaskServiceInitializer : IHostedService
{
    private readonly AgentTaskService _agentTaskService;
    private readonly IServiceProvider _serviceProvider;
    private readonly TaskSearchService _taskSearchService;

    public AgentTaskServiceInitializer(
        AgentTaskService agentTaskService,
        IServiceProvider serviceProvider,
        TaskSearchService taskSearchService)
    {
        _agentTaskService = agentTaskService;
        _serviceProvider = serviceProvider;
        _taskSearchService = taskSearchService;
    }

    private string GetAgentInstructions()
    {
        return 
            "You are a helpful assistant that manages tasks " +
            "and answer the user's questions about how to use the system using the manual. " +
            "Each task has a title, description, due date, and iscompleted status." +
            "The title is not descriptive for the task." +
            "The description describes what the task is for and what the user is supposed to do." +
            "The due date is when the task is supposed to be done by." +
            "The iscompleted status shows if the task is done or not." +
            "Use ToolsPlugin - 'Today' function to get the today's date." +
            "Use ToolsPlugin - 'Clear' function to clear the chat history and context or when asked to start over or forget everything." +
            "Use TasksSearchPlugin  - 'SearchAsync' function to search for specific task or tasks." +
            "Use it to answer questions about tasks such as: " +
            "- Are there tasks like <description>?" +
            "- Do I have to do something like <description>?" +
            "- Do I have to do something like <description>? which is overdue and not completed?" +
            "- Get me all tasks that are like <description> and are overdue and completed." +
            "Use TaskServicePlugin to get all tasks, get a task by title, mark a task as complete, " +
            "Use TaskServicePlugin - 'GetAllTasks' function to answer questions about tasks such as: " +
            "- Are there any tasks due today?" +
            "- Are there any overdue tasks?" +
            "- Do I have any tasks that are not completed?" +
            "- Do I have any tasks that are overdue and not completed?" +
            "- Get me all overdue tasks." +
            "- Get me all tasks that are overdue and not completed." +
            "Use TaskServicePlugin to delete a task (Make sure to confirm with the user before deleting), " +
            "or to create a new one. " +
            "ALWAYS answer in this format: " +
            ReadFileResource("AskResponse.json") +
            "This is the Task Manager Manual for reference: " +
            ReadFileResource("Manual.txt");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var toolsPluginObject = new ToolsPlugin(_agentTaskService);

            // Create the embeding generator and initialize the TaskSearchService
            var embeddingGenerator = CreateEmbeddingGenerator();
            await _taskSearchService.InitializeAsync(embeddingGenerator);

            // Load tasks into the vector store
            var tasks = scope.ServiceProvider.GetRequiredService<ITaskService>().GetTasks();
            await _taskSearchService.AddVectorStoreTasksEntries(tasks);

            var taskServicePluginObject = new TaskServicePlugin(_serviceProvider);
            var taskSearchPluginObject = new TaskSearchPlugin(_taskSearchService);

            var client = CreateChatClient();

            ChatClientAgent agent = client.CreateAIAgent(
                instructions: GetAgentInstructions(),
                name: "TaskManagerAgent",
                tools: [
                    AIFunctionFactory.Create(toolsPluginObject.Today),
                    AIFunctionFactory.Create(toolsPluginObject.Clear),
                    AIFunctionFactory.Create(taskServicePluginObject.GetTasksAsync),
                    AIFunctionFactory.Create(taskServicePluginObject.CreateAsync),
                    AIFunctionFactory.Create(taskServicePluginObject.FindByNameAsync),
                    AIFunctionFactory.Create(taskServicePluginObject.MarkCompleteAsync),
                    AIFunctionFactory.Create(taskServicePluginObject.DeleteAsync),
                    AIFunctionFactory.Create(taskSearchPluginObject.SearchAsync)
                ]);

            _agentTaskService.Initialize(agent);
        }
        await Task.CompletedTask;
    }


    // Read a file from the base directory. 
    private string ReadFileResource(string fileName)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        if (File.Exists(filePath))
            return File.ReadAllText(filePath);
        else
            throw new InvalidOperationException($"{fileName} file not found.");
    }

    private IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator()
    {
        var client = new AzureOpenAIClient(
            new Uri(TaskManagerConfiguration.AzureOpenAIEmbeddings.Endpoint), new AzureCliCredential())
            .GetEmbeddingClient(TaskManagerConfiguration.AzureOpenAIEmbeddings.DeploymentName)
            .AsIEmbeddingGenerator(1536);

        return client;
    }

        private static OpenAI.Chat.ChatClient CreateChatClient()
    {
        return new AzureOpenAIClient(
            new Uri(TaskManagerConfiguration.AzureOpenAI.Endpoint),
            new System.ClientModel.ApiKeyCredential(TaskManagerConfiguration.AzureOpenAI.ApiKey))
            .GetChatClient(TaskManagerConfiguration.AzureOpenAI.DeploymentName);
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask; // kill the agent
}