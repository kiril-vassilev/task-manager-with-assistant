using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using TaskManager.Domain;

namespace TaskManager.BLL;

public class AgentTaskService
{
    private Kernel? _kernel;
    private ChatCompletionAgent? _agent;
    private ChatHistory? _history;


    public AgentTaskService()
    {
        // Empty constructor for DI
    }

    // Initialization method (no plugin import)
    public void Initialize(Kernel kernel, ChatCompletionAgent agent, ChatHistory history)
    {
        _kernel = kernel;
        _agent = agent;
        _history = history;
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        if (_agent == null)
            throw new InvalidOperationException("Agent not initialized.");

        if (_kernel == null)
            throw new InvalidOperationException("Kernel not initialized.");

        if (_history == null)   
            throw new InvalidOperationException("History not initialized.");


        ChatMessageContent message = new(AuthorRole.User, question);
        _history.Add(message);

        await foreach (var response in _agent.InvokeAsync(_history))
        {
            var formattedResponse = FormatResponse(response);
            _history.Add(response);
            
            FunctionResultContent[] functionResults = await ProcessFunctionCalls(response, _kernel).ToArrayAsync();

            foreach (ChatMessageContent functionResult in functionResults.Select(result => result.ToChatMessage()))
            {
                formattedResponse += FormatResponse(functionResult);
                _history.Add(functionResult);
            }

            return formattedResponse;
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
            else if (item is FunctionResultContent functionResult)
            {
                //ret += $"  [{item.GetType().Name}] {functionResult.CallId} - {functionResult.Result?.AsJson() ?? "*"}";
                if (functionResult.Result is IEnumerable<TaskItem> tasks)
                {
                    foreach (var task in tasks)
                        ret += $"Task: Title={task.Title}: Description={task.Description}: DueDate={task.DueDate:dd//MMM//yyyy}: IsCompleted={task.IsCompleted}\n";
                }
                else if (functionResult.Result is TaskItem task)
                {
                    ret += $"Task: Title={task.Title}: Description={task.Description}: DueDate={task.DueDate:dd//MMM//yyyy}: IsCompleted={task.IsCompleted}\n";
                }
                else if (functionResult.Result is null)
                {
                    ret += $"No task found.\n";
                }
                else
                {
                    ret += $"{functionResult.Result?.AsJson() ?? "*"}\n";
                }
            }
        }

        return ret;
    }

    private async IAsyncEnumerable<FunctionResultContent> ProcessFunctionCalls(ChatMessageContent response, Kernel kernel)
    {
        foreach (FunctionCallContent functionCall in response.Items.OfType<FunctionCallContent>())
        {
            yield return await functionCall.InvokeAsync(kernel);
        }
    }

}