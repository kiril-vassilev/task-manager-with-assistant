using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace TaskManager.BLL;

public class AgentTaskService
{
    private Kernel? _kernel;
    private ChatCompletionAgent? _agent;

    public AgentTaskService()
    {
        // Empty constructor for DI
    }

    // Initialization method (no plugin import)
    public void Initialize(Kernel kernel, ChatCompletionAgent agent)
    {
        _kernel = kernel;
        _agent = agent;
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        if (_agent == null)
            throw new InvalidOperationException("Agent not initialized.");

        ChatMessageContent message = new(AuthorRole.User, question);

        await foreach (var response in _agent.InvokeAsync(message))
        {
            return FormatResponse(response);
        }
        throw new InvalidOperationException("No response from agent.");
    }

    private string FormatResponse(ChatMessageContent response)
    {
        string ret = "";

        foreach (var item in response.Items)
        {
            if (item is TextContent text)
            {
                ret += $"  [{item.GetType().Name}] {text.Text}\n";
            }
            else if (item is FunctionCallContent functionCall)
            {
                ret += $"  [{item.GetType().Name}] {functionCall.Id}\n";
            }
        }

        return ret;
    }
}