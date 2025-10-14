using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.ChatCompletion;

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


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var kernel = CreateKernel();
            var toolsPlugin = KernelPluginFactory.CreateFromObject(new ToolsPlugin(_agentTaskService));

            // Create the embeding generator and initialize the TaskSearchService
            var embeddingGenerator = CreateEmbeddingGenerator();
            await _taskSearchService.InitializeAsync(embeddingGenerator);

            // Load tasks into the vector store
            var tasks = scope.ServiceProvider.GetRequiredService<ITaskService>().GetTasks();
            await _taskSearchService.AddVectorStoreTasksEntries(tasks);

            var taskServicePlugin = KernelPluginFactory.CreateFromObject(new TaskServicePlugin(_serviceProvider));
            var textSearchPlugin = KernelPluginFactory.CreateFromObject(new TaskSearchPlugin(_taskSearchService));

            kernel.Plugins.Add(toolsPlugin);
            kernel.Plugins.Add(taskServicePlugin);
            kernel.Plugins.Add(textSearchPlugin);

            ChatCompletionAgent agent =
                new()
                {
                    Instructions =
                    "You are a helpful assistant that manages tasks " +
                    "and answer the user's questions about how to use the system using the manual. " +
                    "Each task has a title, description, due date, and iscompleted status." +
                    "The title is not descriptive for the task." +
                    "The description describes what the task is for and what the user is supposed to do." +
                    "The due date is when the task is supposed to be done by." +
                    "The iscompleted status shows if the task is done or not." +
                    "Use ToolsPlugin - 'Today' function to get the today's date, when asked or when creating a new task with due date today." +
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
                    "Always answer in this format: " +
                    ReadFileResource("AskResponse.json") +
                    "This is the Task Manager Manual for reference: " +
                    ReadFileResource("Manual.txt"),
                    Name = "TaskManagerAgent",
                    Kernel = kernel,
                    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false) }),
                };

            _agentTaskService.Initialize(kernel, agent);
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

    private Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            TaskManagerConfiguration.AzureOpenAI.DeploymentName,
            TaskManagerConfiguration.AzureOpenAI.Endpoint,
            TaskManagerConfiguration.AzureOpenAI.ApiKey
        );

        return builder.Build();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask; // kill the agent
}