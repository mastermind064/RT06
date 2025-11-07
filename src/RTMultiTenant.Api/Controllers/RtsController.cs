using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Rt;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/rts")]
public class RtsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public RtsController(AppDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRtAsync([FromBody] CreateRtRequest request, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Rts.AnyAsync(rt =>
            rt.RtNumber == request.RtNumber &&
            rt.RwNumber == request.RwNumber &&
            rt.VillageName == request.VillageName &&
            rt.SubdistrictName == request.SubdistrictName &&
            rt.CityName == request.CityName &&
            rt.ProvinceName == request.ProvinceName, cancellationToken);

        if (exists)
        {
            return Conflict("RT context already exists");
        }

        var now = DateTime.UtcNow;
        var rt = new Rt
        {
            RtId = Guid.NewGuid(),
            RtNumber = request.RtNumber,
            RwNumber = request.RwNumber,
            VillageName = request.VillageName,
            SubdistrictName = request.SubdistrictName,
            CityName = request.CityName,
            ProvinceName = request.ProvinceName,
            AddressDetail = request.AddressDetail,
            CreatedAt = now,
            UpdatedAt = now
        };
        Console.WriteLine($"RtId: {rt.RtId}");  

        var adminUser = new User
        {
            UserId = Guid.NewGuid(),
            RtId = rt.RtId,
            Username = request.AdminUser.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminUser.Password),
            Role = "ADMIN",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.Rts.Add(rt);
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var token = _jwtTokenService.GenerateToken(adminUser);
        return Ok(new
        {
            rt.RtId,
            adminUser.UserId,
            adminUser.Username,
            Token = token
        });
    }
}
