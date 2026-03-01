using PromptApp.Model;
using PromptApp.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PromptsController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly PromptService _promptService;

    public PromptsController(RabbitMqPublisher publisher, PromptService promptService)
    {
        _publisher = publisher;
        _promptService = promptService;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitPrompt([FromBody] PromptRequest request)
    {
        var prompt = await _promptService.AddAsync(new Prompt { Content = request.Content });
        await _publisher.PublishAsync(prompt);
        return Accepted(new { prompt.Id, message = "Prompt enqueued for processing" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskStatus(Guid id)
    {
        var prompt = await _promptService.GetByIdAsync(id);
        if (prompt == null) return NotFound();
        return Ok(new { prompt.Id, prompt.State, prompt.Result });
    }
}