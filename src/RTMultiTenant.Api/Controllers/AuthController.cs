using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    private readonly PasswordResetService _passwordResetService;

    public AuthController(AppDbContext dbContext, IJwtTokenService jwtTokenService, ITenantProvider tenantProvider,
        PasswordResetService passwordResetService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _tenantProvider = tenantProvider;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var defaultPic = "/uploads/default/avatar.png";
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

        if (user.Role == "WARGA" && user.ResidentId != null)
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
            PicPath = resident.PicPath.IsNullOrEmpty() ? defaultPic : resident.PicPath
        };
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken,
        [FromServices] IEmailSender emailSender)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
            return BadRequest("Username atau email wajib diisi");
        if (string.IsNullOrWhiteSpace(request.Blok))
            return BadRequest("Blok wajib diisi");

        bool looksEmail = request.UsernameOrEmail.Contains('@');
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            looksEmail ? u.Email == request.UsernameOrEmail : u.Username == request.UsernameOrEmail,
            cancellationToken);
        if (user is null)
            return NotFound(looksEmail ? "Email tidak terdaftar" : "Akun tidak ditemukan");

        if (!user.ResidentId.HasValue)
        {
            return BadRequest("Akun tidak memiliki data warga");
        }

        var resident = await _dbContext.Residents.FirstOrDefaultAsync(r => r.ResidentId == user.ResidentId.Value, cancellationToken);
        if (resident is null)
        {
            return NotFound("Data warga tidak ditemukan");
        }
        if (!string.Equals(resident.Blok, request.Blok, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound("Blok tidak cocok dengan akun");
        }

        var token = _passwordResetService.Generate(user.UserId);
        var resetUrl = $"/reset-password?token={token}"; // FE path

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest("Email belum diisi pada akun ini");
        }

        var subject = "Reset Password Portal RT";
        var body = $"<p>Anda menerima email ini karena ada permintaan reset password.</p><p>Klik tautan berikut untuk mengubah password:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p><p>Tautan berlaku selama 2 jam.</p>";
        var sent = await emailSender.SendAsync(user.Email!, subject, body, cancellationToken);
        Console.WriteLine($"[PasswordReset] Link for {user.Username}: {resetUrl} (email sent: {sent})");

        return Accepted(new { Message = sent ? "Tautan reset telah dikirim ke email Anda" : "SMTP belum dikonfigurasi, gunakan tautan dev untuk reset", EmailSent = sent, DevResetPath = resetUrl });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("Token dan password baru wajib diisi");

        var userId = _passwordResetService.Validate(request.Token);
        if (!userId.HasValue)
        {
            return BadRequest("Token tidak valid atau sudah kedaluwarsa");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _passwordResetService.Invalidate(request.Token);
        return NoContent();
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
                Email = request.Email,
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
