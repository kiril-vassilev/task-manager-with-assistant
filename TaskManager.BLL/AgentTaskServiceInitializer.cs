using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.Extensions.DependencyInjection;

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
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

            var kernel = CreateKernel();
            var plugin = KernelPluginFactory.CreateFromObject(new TaskServicePlugin(taskService));

            kernel.Plugins.Add(plugin);

            ChatCompletionAgent agent =
                new()
                {
                    Instructions = "You are a helpful assistant that manages tasks. Use the provided functions to get information about tasks or create new ones.",
                    Name = "TaskManagerAgent",
                    Kernel = kernel,
                    Arguments = new KernelArguments(new PromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
                };

            _agentTaskService.Initialize(kernel, agent);
        }
        await Task.CompletedTask;
    }

    private Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            "TestConfiguration.AzureOpenAI.ChatDeploymentName",
            "TestConfiguration.AzureOpenAI.Endpoint",
            "TestConfiguration.AzureOpenAI.ApiKey"
        );

        return builder.Build();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask; // kill the agent
}