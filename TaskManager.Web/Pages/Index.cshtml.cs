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
        Console.WriteLine($"Id: {Id}");

        Console.WriteLine("OnPostAsync called");
        Console.WriteLine($"Title: {Title}");
        Console.WriteLine($"DueDate: {DueDate}");
        Console.WriteLine($"Description: {Description}");

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
            Console.WriteLine("OnPostCompleteAsync");
            Console.WriteLine($"Id: {Id}");

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

        // Log
        Console.WriteLine($"User asked: {ChatbotQuestion}");

        // Call AgentController API
        string? answer = null;
        try
        {
            var payload = new { question = ChatbotQuestion };
            var response = await _http.PostAsJsonAsync("/api/agent/ask", payload);
            response.EnsureSuccessStatusCode();
            answer = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            answer = $"Error: {ex.Message}";
        }

    ChatbotAnswer = answer;

    // Refresh the list of tasks after chatbot interaction
    Tasks = await _http.GetFromJsonAsync<List<TaskItem>>("/api/tasks") ?? [];

    return new JsonResult(new { answer = ChatbotAnswer, tasks = Tasks });
    }
}

