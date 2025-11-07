using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Contributions;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Extensions;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ITenantProvider _tenantProvider;
        private readonly EventPublisher _eventPublisher;
        public UsersController(AppDbContext dbContext, ITenantProvider tenantProvider, EventPublisher eventPublisher)
        {
            _dbContext = dbContext;
            _tenantProvider = tenantProvider;
            _eventPublisher = eventPublisher;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetUsersAsync(CancellationToken cancellationToken)
        {
            var rtId = _tenantProvider.GetRtId();
            var query = _dbContext.Users.Where(c => c.RtId == rtId && c.Role != "ADMIN");

            var results = await query
            .OrderByDescending(c => c.Username)
            .Select(c => new
            {
                c.UserId,
                c.Username,
                c.ResidentId,
                c.Role,
                c.IsActive,
                c.CreatedAt
            }).ToListAsync(cancellationToken);

            return Ok(results);
        }

        //TODO: Update user status (active/inactive)
        [HttpPatch("{userId:guid}/status/{isActive:bool}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateUsersAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
        {
            var rtId = _tenantProvider.GetRtId();
            var query = _dbContext.Users.Where(c => c.RtId == rtId && c.Role != "ADMIN" && c.UserId == userId && c.IsActive != isActive);

            var user = await query.FirstOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                return NotFound("User not found");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.AppendAsync("USER", user.UserId, "UserStatusUpdated", new
            {
                user.IsActive
            }, userId, cancellationToken);

            return Ok(new { user.UserId, user.IsActive });
        }
    }
}
