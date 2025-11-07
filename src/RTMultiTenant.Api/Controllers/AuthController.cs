using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Auth;
using RTMultiTenant.Api.Entities;
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

        var resident = new Resident();

        if (user.Role == "WARGA" && user.ResidentId is null)
        {
            resident = await _dbContext.Residents.FirstOrDefaultAsync(r => r.ResidentId == user.ResidentId, cancellationToken);
            if (resident is null)
            {
                return Unauthorized("Invalid resident");
            }
        }        

        var rt = await _dbContext.Rts.FirstOrDefaultAsync(r => r.RtId == user.RtId, cancellationToken);
        if (rt is null)
        {
            return Unauthorized("invalid RT");
        }

        var token = _jwtTokenService.GenerateToken(user);
        return new LoginResponse
        {
            Token = token,
            RtId = user.RtId,
            UserId = user.UserId,
            Role = user.Role,
            Username = user.Username + " - " + resident.Blok,
            RtRw = rt.RtNumber + "/RW " + rt.RwNumber,
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

        // MULAI TRANSAKSI DATABASE
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            Guid residentIdToUse;

            if (request.ResidentId.HasValue)
            {
                // CASE 1: ADMIN MAU LINK USER KE RESIDENT YANG SUDAH ADA
                var resident = await _dbContext.Residents
                    .FirstOrDefaultAsync(r => r.ResidentId == request.ResidentId.Value, cancellationToken);

                if (resident is null || resident.RtId != rtId)
                {
                    return BadRequest("Resident not found in this RT");
                }

                residentIdToUse = resident.ResidentId;
            }
            else
            {
                // CASE 2: RESIDENT BARU (DUMMY) DIBUAT OTOMATIS
                var newResident = new Resident
                {
                    ResidentId = Guid.NewGuid(),
                    RtId = rtId,
                    NationalIdNumber = "-",
                    FullName = "-",
                    BirthDate = DateTime.UtcNow,
                    Gender = "-",
                    Blok = "-",
                    PhoneNumber = "-",
                    ApprovalStatus = "DRAFT",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.Residents.Add(newResident);
                await _dbContext.SaveChangesAsync(cancellationToken); // simpan sementara di dalam transaksi

                residentIdToUse = newResident.ResidentId;
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
                ResidentId = residentIdToUse, // <-- penting: pastikan ke residentId yang benar
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // KALAU SEMUA OK -> COMMIT
            await transaction.CommitAsync(cancellationToken);

            return CreatedAtAction(nameof(GetUserAsync), new { userId = user.UserId }, new
            {
                user.UserId,
                user.Username,
                user.Role,
                user.IsActive
            });
        }
        catch (Exception ex)
        {
            // KALAU ADA ERROR -> ROLLBACK
            await transaction.RollbackAsync(cancellationToken);

            // boleh kamu log ex di logger
            // _logger.LogError(ex, "Failed to register user");

            return StatusCode(StatusCodes.Status500InternalServerError,
                "Failed to register user");
        }
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
