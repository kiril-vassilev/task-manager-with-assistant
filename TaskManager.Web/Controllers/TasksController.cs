using Microsoft.AspNetCore.Mvc;
using TaskManager.BLL;
using TaskManager.Domain;

namespace TaskManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _service;
    public TasksController(ITaskService service) => _service = service;

    [HttpGet]
    public ActionResult<IEnumerable<TaskItem>> GetAll() => Ok(_service.GetTasks());

    [HttpPost]
    public ActionResult<TaskItem> Create([FromBody] TaskItem task)
    {
        Console.WriteLine($"Creating task: {task.Title}");

        var created = _service.Create(task);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id}/complete")]
    public IActionResult Complete(int id)
    {
        _service.MarkComplete(id);
        return NoContent();
    }
    
    [HttpGet("find")]
    public ActionResult<TaskItem> FindByName([FromQuery] string name)
    {
        var task = _service.FindByName(name);
        if (task == null) return NotFound();
        return Ok(task);
    }    
}
