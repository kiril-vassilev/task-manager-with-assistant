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
    public async Task<ActionResult<string>> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question cannot be empty.");
        try
        {
            var response = await _agentService.AskQuestionAsync(request.Question);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, ex.Message);
        }
    }
}
