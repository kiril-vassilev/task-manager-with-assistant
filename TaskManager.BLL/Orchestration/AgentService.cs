using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class AgentService
{
    private GuardianAgentExecutor? _guardianExecutor;
    private WorkerAgentExecutor? _workerExecutor;
    private Workflow? _workflow;

    public AgentService()
    {
        // Empty constructor for DI
    }

    public void Initialize(GuardianAgentExecutor guardianExecutor, WorkerAgentExecutor workerExecutor)
    {
        _guardianExecutor = guardianExecutor;
        _workerExecutor = workerExecutor;

        var threatDetectedExecutor = new ThreatDetectedExecutor();
        var noThreatDetectedExecutor = new NoThreatDetectedExecutor();

        _workflow = new WorkflowBuilder(_guardianExecutor)
            .AddEdge(_guardianExecutor, noThreatDetectedExecutor, condition: static (GuardianResponse? response) => response != null && !response.IsThreatDetected)
            .AddEdge(_guardianExecutor, threatDetectedExecutor, condition: static (GuardianResponse? response) => response != null && response.IsThreatDetected)
            .AddEdge(noThreatDetectedExecutor, _workerExecutor)
            .WithOutputFrom(_workerExecutor, threatDetectedExecutor)
            .Build();

        // Alternative simpler workflow without threat detection
        //
        // _workflow = new WorkflowBuilder(_workerExecutor)
        //     .WithOutputFrom(_workerExecutor)
        //     .Build();

        // Uncomment to visualize the workflow
        //
        // var mermaid = _workflow.ToMermaidString();
        // var dot = _workflow.ToDotString();
        // Console.WriteLine("======= Workflow Visualization =======");
        // Console.WriteLine("======= Workflow (Mermaid) =======");
        // Console.WriteLine(mermaid);
        // Console.WriteLine("======= Workflow (DOT) =======");
        // Console.WriteLine(dot);
        // Console.WriteLine("======= Workflow Visualization =======");
    }
    
    public async Task<AskResponse> AskQuestionAsync(string question)
    {
        if (_workflow == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a Workflow.");

        await using StreamingRun run = await InProcessExecution.StreamAsync(_workflow, new ChatMessage(ChatRole.User, question));
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            switch (evt)
            {
                case ExecutorCompletedEvent executorCompletedEvent:
                    if (executorCompletedEvent.Data is GuardianResponse guardianResponse)
                        Console.WriteLine($"Executor Completed: {guardianResponse.Answer}");
                    if (executorCompletedEvent.Data is AskResponse askResponse)
                        Console.WriteLine($"Executor Completed: {askResponse.Answer}");
                    break;

                case WorkflowErrorEvent errorEvent:
                    return new AskResponse { Answer = $"Error: {errorEvent}", Tasks = [] };

                case ExecutorFailedEvent executorErrorEvent: 
                    return new AskResponse { Answer = $"Executor Error: {executorErrorEvent.Data?.Message ?? "Unknown error"}", Tasks = [] };

                case WorkflowOutputEvent outputEvent:
                    if (outputEvent.Data is AskResponse outputAskResponse)
                        return outputAskResponse;

                    throw new InvalidOperationException("Unexpected output type from workflow.");
            }

        throw new InvalidOperationException("The workflow did not produce a valid output.");
    }

    public void CreateClearHistory()
    {
        if (_workerExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a WorkerAgentExecutor.");

        _workerExecutor.CreateClearHistory();

        if (_guardianExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a GuardianAgentExecutor.");

        _guardianExecutor.CreateClearHistory();
    }
}
