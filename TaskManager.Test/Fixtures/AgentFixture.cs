using System;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Azure.AI.OpenAI;
using TaskManager.BLL;
using TaskManager.Domain;

#nullable enable

namespace TaskManager.Test.Fixtures;

public class AgentFixture : IDisposable
{
    public readonly InMemoryTaskServicePlugin _taskServicePlugin;  
    
    private readonly AgentConfiguration _agentConfiguration;
    private ChatClientAgent? _workerAgent;
    private ChatClientAgent? _guardianAgent;


    public AgentFixture()
    {
        IConfigurationRoot configRoot = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly())
        .Build();

        TaskManagerConfiguration.Initialize(configRoot);

        _agentConfiguration = new AgentConfiguration();
        _taskServicePlugin = new InMemoryTaskServicePlugin();
    }
    
    public async Task<ChatClientAgent> GetGuardianAgentAsync()
    {
        if (_guardianAgent != null)
            return _guardianAgent;
        
        var client = _agentConfiguration.CreateChatClient();

        _guardianAgent = client.AsAIAgent(
            instructions: _agentConfiguration.GetGuardianAgentInstructions(),
            name: "TaskManagerGuardian");

        return _guardianAgent;
    }

    public async Task<ChatClientAgent> GetWorkerAgentAsync()
    {
        if (_workerAgent != null)
            return _workerAgent;
        
        var client = _agentConfiguration.CreateChatClient();

        // Create instances of the in-memory plugins we've added for tests
        var taskSearchPlugin = new InMemoryTaskSearchPlugin(_taskServicePlugin);

        // Construct strongly-typed delegates to avoid method-group -> System.Delegate conversion errors
        var tools = new[]
        {
            AIFunctionFactory.Create(new Func<Task<string>>(toolsPlugin.Today)),
            AIFunctionFactory.Create(new Func<Task<string>>(toolsPlugin.Clear)),
            AIFunctionFactory.Create(new Func<Task<System.Collections.Generic.IEnumerable<TaskItem>>>(_taskServicePlugin.GetTasksAsync), name: "GetAllTasks"),
            AIFunctionFactory.Create(new Func<TaskItem, Task<TaskItem>>(_taskServicePlugin.CreateAsync), name: "Create", description: "Creates a new task. Ask the user for title, description, and due date in the future or today."),
            AIFunctionFactory.Create(new Func<string, Task<TaskItem?>>(_taskServicePlugin.FindByNameAsync), name: "FindByTitle", description: "Finds a task by title. Do not use it for searching by description or other fields."),
            AIFunctionFactory.Create(new Func<string, Task<TaskItem?>>(_taskServicePlugin.MarkCompleteAsync), name: "MarkComplete", description: "Marks a task as complete."),
            AIFunctionFactory.Create(new Func<string, Task<bool>>(_taskServicePlugin.DeleteAsync), name: "Delete", description: "Deletes a task. It is IMPORTANT to confirm with the user before deleting one or more tasks."),
            AIFunctionFactory.Create(new Func<string, Task<System.Collections.Generic.IEnumerable<TaskItem>>>(taskSearchPlugin.SearchAsync), name: "Search")
        };

        _workerAgent = client.AsAIAgent(
            instructions: _agentConfiguration.GetWorkerAgentInstructions(),
            name: "TaskManagerAgent",
            tools: tools);

        return _workerAgent;
    }

    public void Dispose()
    {
        // Cleanup if needed
        _guardianAgent = null;
        _workerAgent = null;
    }
}
