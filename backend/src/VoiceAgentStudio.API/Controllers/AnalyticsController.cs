using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceAgentStudio.Application.Analytics;
using VoiceAgentStudio.Application.Common.Interfaces;

namespace VoiceAgentStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsController(IAnalyticsService analyticsService, ICurrentUserService currentUser)
    {
        _analyticsService = analyticsService;
        _currentUser = currentUser;
    }

    /// <summary>Get full analytics dashboard data for the authenticated user</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Not authenticated.");

        var data = await _analyticsService.GetFullAnalyticsAsync(userId, ct);
        return Ok(data);
    }
}
