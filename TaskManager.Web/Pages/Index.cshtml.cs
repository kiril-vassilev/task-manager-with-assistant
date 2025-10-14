using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using TaskManager.Domain;
using System.Text.Json;

public class IndexModel : PageModel
{
    private readonly HttpClient _http;
    public List<TaskItem> Tasks { get; set; } = new();

    [BindProperty]
    public int Id { get; set; }
    [BindProperty]
    public string Title { get; set; } = string.Empty;
    [BindProperty]
    public DateTime DueDate { get; set; }
    [BindProperty]
    public string? Description { get; set; }


    public string? ChatbotQuestion { get; set; }
    // Property for chatbot answer
    public string? ChatbotAnswer { get; set; }



    public IndexModel(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("TaskApi");
    }

    public async Task OnGetAsync()
    {
        Tasks = await _http.GetFromJsonAsync<List<TaskItem>>("/api/tasks") ?? [];
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id <= 0)
        {
            // Create new task
            var body = new TaskItem
            {
                Title = Title,
                DueDate = DueDate,
                Description = Description,
                IsCompleted = false
            };

            var resp = await _http.PostAsJsonAsync("/api/tasks", body);
            resp.EnsureSuccessStatusCode();
            return RedirectToPage();
        }
        else
        {
            // Complete task
            var resp = await _http.PutAsync($"/api/tasks/{Id}/complete", null);
            resp.EnsureSuccessStatusCode();
            return RedirectToPage();
        }
    }

    // AJAX handler for chatbot question
    public async Task<IActionResult> OnPostUserAsksAsync()
    {
        // Read JSON body
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body);

        ChatbotQuestion = json.RootElement.GetProperty("ChatbotQuestion").GetString();

        // Call AgentController API and expect structured AskResponse JSON
        TaskManager.Domain.AskResponse? askResponse = null;
        try
        {
            var payload = new { question = ChatbotQuestion };
            var response = await _http.PostAsJsonAsync("/api/agent/ask", payload);
            // Even if non-success, try to deserialize structured response
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                askResponse = JsonSerializer.Deserialize<TaskManager.Domain.AskResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                // Fallback: wrap raw content as answer
                askResponse = new TaskManager.Domain.AskResponse { Answer = content ?? string.Empty, Tasks = new List<TaskManager.Domain.TaskItem>() };
            }
        }
        catch (Exception ex)
        {
            askResponse = new TaskManager.Domain.AskResponse { Answer = $"Error: {ex.Message}", Tasks = new List<TaskManager.Domain.TaskItem>() };
        }

        ChatbotAnswer = askResponse?.Answer;

        // Refresh the list of tasks after chatbot interaction
        Tasks = (askResponse?.Tasks != null && askResponse.Tasks.Count > 0)
            ? askResponse.Tasks
            : new List<TaskItem>(); //  await _http.GetFromJsonAsync<List<TaskItem>>("/api/tasks") ?? new List<TaskItem>();

        return new JsonResult(new { answer = ChatbotAnswer, tasks = Tasks });
    }
}

