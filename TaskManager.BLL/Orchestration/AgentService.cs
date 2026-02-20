using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class AgentService
{
    private GuardianAgentExecutor? _guardianExecutor;
    private FirstLineAgentExecutor? _firstLineExecutor;
    private QnAAgentExecutor? _qnaExecutor;
    private WorkerAgentExecutor? _workerExecutor;
    private Workflow? _workflow;

    public AgentService()
    {
        // Empty constructor for DI
    }

    public async Task InitializeAsync(
        GuardianAgentExecutor guardianExecutor, 
        FirstLineAgentExecutor firstLineExecutor,
        QnAAgentExecutor qnaExecutor,
        WorkerAgentExecutor workerExecutor)
    {
        _guardianExecutor = guardianExecutor;
        _firstLineExecutor = firstLineExecutor;
        _qnaExecutor = qnaExecutor;
        _workerExecutor = workerExecutor;

        // Create a new session for the agents 
        await CreateClearHistoryAsync();

        var threatDetectedExecutor = new ThreatDetectedExecutor();
        var noThreatDetectedExecutor = new NoThreatDetectedExecutor();

        _workflow = new WorkflowBuilder(_guardianExecutor)
            .AddEdge(_guardianExecutor, threatDetectedExecutor, condition: static (GuardianResponse? response) => response != null && response.IsThreatDetected)
            .AddEdge(_guardianExecutor, noThreatDetectedExecutor, condition: static (GuardianResponse? response) => response != null && !response.IsThreatDetected)
            .AddEdge(noThreatDetectedExecutor, _firstLineExecutor)
            .AddEdge(_firstLineExecutor, _qnaExecutor, condition: static (FirstLineResponse? response) => response != null && response.Redirect == RedirectType.QnAAgent)
            .AddEdge(_firstLineExecutor, _workerExecutor, condition: static (FirstLineResponse? response) => response != null && (response.Redirect == RedirectType.WorkerAgent || response.Redirect == RedirectType.None))
            .WithOutputFrom(_qnaExecutor, _workerExecutor, threatDetectedExecutor)
            .Build();

        // Alternative simpler workflow without threat detection
        //
        // _workflow = new WorkflowBuilder(_firstLineExecutor)
        //     .AddEdge(_firstLineExecutor, _qnaExecutor, condition: static (FirstLineResponse? response) => response != null && response.Redirect == RedirectType.QnAAgent)
        //     .AddEdge(_firstLineExecutor, _workerExecutor, condition: static (FirstLineResponse? response) => response != null && response.Redirect == RedirectType.WorkerAgent)
        //     .WithOutputFrom(_qnaExecutor, _workerExecutor, threatDetectedExecutor)
        //     .Build();

        // Uncomment to visualize the workflow
        //
        // Console.WriteLine("======= Workflow Visualization =======");
        // Console.WriteLine("======= Workflow (Mermaid) =======");
        // var mermaid = _workflow.ToMermaidString();
        // Console.WriteLine(mermaid);
        // Console.WriteLine("======= Workflow (DOT) =======");
        // var dot = _workflow.ToDotString();
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
                    if (executorCompletedEvent.Data is FirstLineResponse firstLineResponse)
                        Console.WriteLine($"Executor Completed: {firstLineResponse.Answer}");
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

    public async Task CreateClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_firstLineExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a FirstLineAgentExecutor.");

        if (_qnaExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a QnAAgentExecutor.");

        if (_workerExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a WorkerAgentExecutor.");

        var firstLineAgent = _firstLineExecutor.agent ?? throw new InvalidOperationException("WorkflowTaskService is not initialized with a FirstLineAgentExecutor or its agent is null.");
        
        var firstLineAgentSession = await firstLineAgent.CreateSessionAsync(cancellationToken);

        _firstLineExecutor.session = firstLineAgentSession;
        _qnaExecutor.session = firstLineAgentSession;
        _workerExecutor.session = firstLineAgentSession;    
        
        if (_guardianExecutor == null)
            throw new InvalidOperationException("WorkflowTaskService is not initialized with a GuardianAgentExecutor.");

        _guardianExecutor.CreateClearHistory();
        
    }
}
