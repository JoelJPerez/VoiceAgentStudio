using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceAgentStudio.Application.Campaigns;
using VoiceAgentStudio.Application.Campaigns.DTOs;

namespace VoiceAgentStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignsController : ControllerBase
{
    private readonly ICampaignService _campaignService;

    public CampaignsController(ICampaignService campaignService)
        => _campaignService = campaignService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CampaignSummaryDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _campaignService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CampaignDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var campaign = await _campaignService.GetByIdAsync(id, ct);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CampaignDto), 201)]
    public async Task<IActionResult> Create([FromBody] CreateCampaignDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var campaign = await _campaignService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = campaign.Id }, campaign);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _campaignService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Import contacts from a CSV file</summary>
    [HttpPost("{id:guid}/contacts/import")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportContacts(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only CSV files are accepted.");

        await using var stream = file.OpenReadStream();
        var count = await _campaignService.ImportContactsAsync(id, stream, ct);

        return Ok(new { imported = count, message = $"{count} contacts imported successfully." });
    }

    /// <summary>Get all contacts for a campaign</summary>
    [HttpGet("{id:guid}/contacts")]
    [ProducesResponseType(typeof(IEnumerable<ContactDto>), 200)]
    public async Task<IActionResult> GetContacts(Guid id, CancellationToken ct)
    {
        var campaign = await _campaignService.GetByIdAsync(id, ct);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    /// <summary>Start campaign execution</summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(CampaignDto), 200)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var campaign = await _campaignService.StartAsync(id, ct);
        return Ok(campaign);
    }

    /// <summary>Pause a running campaign</summary>
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(typeof(CampaignDto), 200)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        var campaign = await _campaignService.PauseAsync(id, ct);
        return Ok(campaign);
    }

    /// <summary>Get all sessions for the live monitor</summary>
    [HttpGet("{id:guid}/sessions")]
    [ProducesResponseType(typeof(IEnumerable<SessionMonitorDto>), 200)]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken ct)
        => Ok(await _campaignService.GetSessionsAsync(id, ct));

    /// <summary>Get full conversation transcript for a session</summary>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(SessionMonitorDto), 200)]
    public async Task<IActionResult> GetSessionDetail(Guid sessionId, CancellationToken ct)
    {
        var session = await _campaignService.GetSessionDetailAsync(sessionId, ct);
        return session is null ? NotFound() : Ok(session);
    }
}
