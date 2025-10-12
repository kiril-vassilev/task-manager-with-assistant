using Microsoft.AspNetCore.Mvc;
using TaskManager.BLL;
using TaskManager.Domain;

namespace TaskManager.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly AgentTaskService _agentService;
    public AgentController(AgentTaskService agentService) => _agentService = agentService;

    [HttpPost("ask")]
    public async Task<ActionResult<AskResponse>> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new AskResponse { Answer = "Question cannot be empty.", Tasks = new List<TaskItem>() });

        try
        {
            var response = await _agentService.AskQuestionAsync(request.Question);
            if (response == null)
            {
                return Ok(new AskResponse { Answer = "No response.", Tasks = new List<TaskItem>() });
            }
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new AskResponse { Answer = ex.Message, Tasks = new List<TaskItem>() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AskResponse { Answer = ex.Message, Tasks = new List<TaskItem>() });
        }
    }

    [HttpPost("clear")]
    public ActionResult ClearHistory()
    {
        try
        {
            _agentService.CreateClearHistory();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
