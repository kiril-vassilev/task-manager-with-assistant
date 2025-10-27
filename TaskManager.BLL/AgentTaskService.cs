using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

using TaskManager.Domain;

namespace TaskManager.BLL;

public class AgentTaskService
{
    private ChatClientAgent? _agent;
    private AgentThread? _thread;

    private const bool IS_DEBUG = false;

    public AgentTaskService()
    {
        // Empty constructor for DI
    }

    public void Initialize(ChatClientAgent agent)
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

    public async Task<AskResponse> AskQuestionAsync(string question)
    {
        if (_agent == null)
            throw new InvalidOperationException("Agent not initialized.");

        if (_thread == null)
            throw new InvalidOperationException("Thread not initialized.");

        var finalAnswer = String.Empty;
        var response = await _agent.RunAsync<AskResponse>(question, _thread);

        if (IS_DEBUG)
        {
            foreach (var message in response.Messages)
            {
                finalAnswer += FormatMessage(message);
            }
        }

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