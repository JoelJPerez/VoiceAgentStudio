using Microsoft.AspNetCore.Mvc;
using VoiceAgentStudio.Application.Auth;

namespace VoiceAgentStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto,
        CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>Register a new operator account</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto,
        CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(dto, ct);
        return CreatedAtAction(nameof(Login), result);
    }
}
