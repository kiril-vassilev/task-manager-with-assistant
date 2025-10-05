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


namespace TaskManager.BLL;

public class AgentTaskServiceInitializer : IHostedService
{
    private readonly AgentTaskService _agentTaskService;
    private readonly IServiceProvider _serviceProvider;

    public AgentTaskServiceInitializer(AgentTaskService agentTaskService, IServiceProvider serviceProvider)
    {
        _agentTaskService = agentTaskService;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var kernel = CreateKernel();
            var embeddingGenerator = CreateEmbeddingGenerator();
            var taskSearchService = new TaskSearchService(embeddingGenerator);

            // Load tasks into the vector store
            var tasks = scope.ServiceProvider.GetRequiredService<ITaskService>().GetTasks();
            await taskSearchService.AddVectorStoreTasksEntries(tasks);

            var taskServicePlugin = KernelPluginFactory.CreateFromObject(new TaskServicePlugin(_serviceProvider));
            var textSearchPlugin = KernelPluginFactory.CreateFromObject(new TaskSearchPlugin(taskSearchService));

            kernel.Plugins.Add(taskServicePlugin);
            kernel.Plugins.Add(textSearchPlugin);

            ChatCompletionAgent agent =
                new()
                {
                    Instructions =
                    "You are a helpful assistant that manages tasks. " +
                    "Each task has a title, description, due date, and iscompleted status." +
                    "The title is not descriptive for the task." +
                    "The description describes what the task is for and what the user is supposed to do." +
                    "The due date is when the task is spupposed to be done by." +
                    "The iscompleted status shows if the task is done or not." +
                    "Use TasksSearch plugin to search for specific task or tasks." +
                    "Use it to answer questions about tasks such as: "+
                    "Are there tasks like <description>?" +
                    "Do I have to do something like <description>?" +
                    "Use TaskServicePlugin to get all tasks, get a task by title, mark a task as complete, delete a task, or to create a new one.",
                    Name = "TaskManagerAgent",
                    Kernel = kernel,
                    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false) }),
                };

            _agentTaskService.Initialize(kernel, agent);
        }
        await Task.CompletedTask;
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