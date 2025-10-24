using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public void Initialize(Kernel kernel, ChatCompletionAgent agent)
    {
        _kernel = kernel;
        _agent = agent;

        CreateClearHistory();
    }

    public void CreateClearHistory()
    {
        _history =
        [
            new ChatMessageContent(AuthorRole.Assistant, "Today is " + DateTime.UtcNow.Date.ToShortDateString()),
        ];
    }

    public async Task<AskResponse> AskQuestionAsync(string question)
    {
        if (_agent == null)
            throw new InvalidOperationException("Agent not initialized.");

        if (_kernel == null)
            throw new InvalidOperationException("Kernel not initialized.");

        if (_history == null)
            throw new InvalidOperationException("History not initialized.");

        var finalTasksResponse = new List<TaskItem>();

        ChatMessageContent message = new(AuthorRole.User, question);
        _history.Add(message);

        await foreach (var response in _agent.InvokeAsync(_history))
        {
            var (agentTasks, agentResponse) = FormatResponse(response);
            _history.Add(response);

            if (agentTasks != null && agentTasks.Any())
                finalTasksResponse.AddRange(agentTasks);

            string? finalResponse = agentResponse;

            FunctionResultContent[] functionResults = await ProcessFunctionCalls(response, _kernel).ToArrayAsync();

            foreach (var functionResult in functionResults)
            {
                // We don't want to add Clear function call to the the just emptied history
                if (functionResult.FunctionName != "Clear")
                    _history.Add(functionResult.ToChatMessage());

                var (resultTasks, resultText) = FormatFunctionResult(functionResult);

                if (resultTasks != null && resultTasks.Any())
                    finalTasksResponse.AddRange(resultTasks);

                if (!string.IsNullOrEmpty(resultText))
                    finalResponse += $"\n{resultText}\n";

                if ((resultTasks == null || !resultTasks.Any()) && string.IsNullOrEmpty(resultText))
                    finalResponse += "\nNo tasks found.\n";
            }

            return new AskResponse
            {
                Answer = finalResponse,
                Tasks = finalTasksResponse
            };
        }

        throw new InvalidOperationException("No response from agent.");
    }
    
    private async IAsyncEnumerable<FunctionResultContent> ProcessFunctionCalls(ChatMessageContent response, Kernel kernel)
    {
        foreach (FunctionCallContent functionCall in response.Items.OfType<FunctionCallContent>())
        {
            yield return await functionCall.InvokeAsync(kernel);
        }
    }    

    private (IEnumerable<TaskItem>, string) FormatResponse(ChatMessageContent response)
    {
        string res = "";
        var tasks = new List<TaskItem>();

        foreach (var item in response.Items)
        {
            if (item is TextContent text)
            {
                var textValue = text.Text?.Trim();

                // Quick check for JSON-ish content and attempt to parse into AskResponse
                if (!string.IsNullOrEmpty(textValue) && (textValue.StartsWith("{") || textValue.StartsWith("[") || textValue.StartsWith("json")))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var jsonResponse = JsonSerializer.Deserialize<AskResponse>(textValue, options);
                        if (jsonResponse != null)
                        {
                            // Include the answer and list items in a concise form
                            res += $"  [{item.GetType().Name}] {jsonResponse.Answer}\n";

                            if (jsonResponse.Tasks != null && jsonResponse.Tasks.Any())
                                tasks.AddRange(jsonResponse.Tasks);

                            continue; // Skip the fallback 
                        }
                    }
                    catch 
                    {
                        // Do nothing so we can fallback to normal text addition
                    }
                }

                // Fallback to normal text addition
                res += $"  [{item.GetType().Name}] {text.Text}\n";
            }
            else if (item is FunctionCallContent functionCall)
            {
                res += $"  [{item.GetType().Name}] [{functionCall.FunctionName}]\n";
            }
        }

        return (tasks, res);
    }

    private (IEnumerable<TaskItem>?, string) FormatFunctionResult(FunctionResultContent functionResult)
    {
        if (functionResult.Result is IEnumerable<TaskItem> tasks)
        {
            return (tasks, string.Empty);
        }
        else if (functionResult.Result is TaskItem task)
        {
            return (new List<TaskItem> { task }, string.Empty);
        }
        else if (functionResult.Result is string s)
        {
            // return null for tasks and the string content
            return (null, s);
        }
        else
        {
            return (null, string.Empty);
        }
    }
}