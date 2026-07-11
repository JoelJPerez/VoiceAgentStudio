using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceAgentStudio.Application.Agents;
using VoiceAgentStudio.Application.Agents.DTOs;

namespace VoiceAgentStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService) => _agentService = agentService;

    /// <summary>Get all agents for the authenticated user</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentSummaryDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var agents = await _agentService.GetAllAsync(ct);
        return Ok(agents);
    }

    /// <summary>Get full agent details by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var agent = await _agentService.GetByIdAsync(id, ct);
        return agent is null ? NotFound() : Ok(agent);
    }

    /// <summary>Create a new AI agent</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AgentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var agent = await _agentService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = agent.Id }, agent);
    }

    /// <summary>Update an existing agent</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AgentDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAgentDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var agent = await _agentService.UpdateAsync(id, dto, ct);
        return Ok(agent);
    }

    /// <summary>Soft-delete an agent</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _agentService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Toggle agent status between Active and Inactive</summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(AgentDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var agent = await _agentService.ToggleStatusAsync(id, ct);
        return Ok(agent);
    }
}
