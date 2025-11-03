using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using TaskManager.Domain;

namespace TaskManager.BLL.Orchestration;

public class WorkerAgentExecutor : Executor<ChatMessage, AskResponse>
{

    private readonly ChatClientAgent? _agent;
    private AgentThread? _thread;

    public WorkerAgentExecutor(ChatClientAgent agent): base("WorkerAgentExecutor")
    {
        _agent = agent;
        CreateClearHistory();
    }

    public void CreateClearHistory()
    {
        if (_agent == null)
            throw new InvalidOperationException("Agent not initialized.");

        _thread = _agent.GetNewThread();
    }
    public override async ValueTask<AskResponse> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (_agent == null)
            throw new InvalidOperationException("WorkerAgentExecutor is not initialized with an ChatClientAgent.");

        if (_thread == null)
            throw new InvalidOperationException("WorkerAgentExecutor is not initialized with an AgentThread.");


        var finalAnswer = String.Empty;
        var response = await _agent.RunAsync<AskResponse>(message.Text, _thread, cancellationToken: cancellationToken);

        if (TaskManagerConfiguration.showAgentThinking)
            foreach (var msg in response.Messages)
                finalAnswer += FormatMessage(msg);

        finalAnswer += response.Result.Answer;

        return new AskResponse
        {
            Answer = finalAnswer,
            Tasks = response.Result.Tasks
        };
    }

    private string FormatMessage(ChatMessage message)
    {
        var res = String.Empty;

        foreach (var content in message.Contents)
        {
            if (content is TextContent textContent)
            {
                res += $"[TextContent] ({TruncateString(textContent.Text)})\n";
            }
            else if (content is FunctionCallContent functionCall)
            {
                res += $"[FunctionCallContent] [{functionCall.Name}]\n";
            }
            else if (content is FunctionResultContent functionResult)
            {
                res += $"[FunctionResultContent] ({TruncateString(functionResult.Result?.ToString() ?? "null")})\n";
            }
        }

        return res;
    }

    private string TruncateString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "none";

        return input.Length > 30 ? input.Substring(0, 30) + "..." : input;
    }    
}