using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Auth;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITenantProvider _tenantProvider;

    public AuthController(AppDbContext dbContext, IJwtTokenService jwtTokenService, ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user is null)
        {
            return Unauthorized("Invalid credentials");
        }

        if (!user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid credentials");
        }

        var token = _jwtTokenService.GenerateToken(user);
        return new LoginResponse
        {
            Token = token,
            RtId = user.RtId,
            UserId = user.UserId,
            Role = user.Role
        };
    }

    [HttpPost("register")]
    [Authorize(Policy = Extensions.AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return Conflict("Username already exists");
        }

        if (request.ResidentId.HasValue)
        {
            var resident = await _dbContext.Residents.FirstOrDefaultAsync(r => r.ResidentId == request.ResidentId.Value, cancellationToken);
            if (resident is null || resident.RtId != rtId)
            {
                return BadRequest("Resident not found in this RT");
            }
        }

        var now = DateTime.UtcNow;
        var user = new Entities.User
        {
            UserId = Guid.NewGuid(),
            RtId = rtId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            ResidentId = request.ResidentId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetUserAsync), new { userId = user.UserId }, new
        {
            user.UserId,
            user.Username,
            user.Role,
            user.IsActive
        });
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = Extensions.AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.UserId,
            user.Username,
            user.Role,
            user.IsActive,
            user.ResidentId
        });
    }

    [HttpPatch("{userId:guid}/status")]
    [Authorize(Policy = Extensions.AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> UpdateStatusAsync(Guid userId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
