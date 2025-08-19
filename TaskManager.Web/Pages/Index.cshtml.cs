using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using TaskManager.Domain;

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

}

