using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Residents;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Extensions;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/residents")]
public class ResidentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    private readonly EventPublisher _eventPublisher;

    public ResidentsController(AppDbContext dbContext, ITenantProvider tenantProvider, EventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
        _eventPublisher = eventPublisher;
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var user = await _dbContext.Users.Include(u => u.Resident).ThenInclude(r => r!.FamilyMembers)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);

        if (user?.Resident is null)
        {
            return Ok(new { Resident = (Resident?)null });
        }

        var resident = user.Resident;
        return Ok(new
        {
            resident.ResidentId,
            resident.NationalIdNumber,
            resident.FullName,
            resident.BirthDate,
            resident.Gender,
            resident.Address,
            resident.PhoneNumber,
            resident.KkDocumentPath,
            resident.ApprovalStatus,
            resident.ApprovalNote,
            FamilyMembers = resident.FamilyMembers.Select(member => new
            {
                member.FamilyMemberId,
                member.FullName,
                member.BirthDate,
                member.Gender,
                member.Relationship
            })
        });
    }

    [HttpPost("me")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> SubmitMyProfileAsync([FromBody] ResidentProfileRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var user = await _dbContext.Users.Include(u => u.Resident).ThenInclude(r => r!.FamilyMembers)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var resident = user.Resident ?? new Resident
        {
            ResidentId = Guid.NewGuid(),
            RtId = rtId,
            CreatedAt = now
        };

        resident.NationalIdNumber = request.NationalIdNumber;
        resident.FullName = request.FullName;
        resident.BirthDate = request.BirthDate;
        resident.Gender = request.Gender;
        resident.Address = request.Address;
        resident.PhoneNumber = request.PhoneNumber;
        resident.KkDocumentPath = request.KkDocumentPath;
        resident.ApprovalStatus = "PENDING";
        resident.ApprovalNote = null;
        resident.UpdatedAt = now;

        if (user.Resident is null)
        {
            _dbContext.Residents.Add(resident);
            user.Resident = resident;
            user.ResidentId = resident.ResidentId;
        }

        var existingMembers = _dbContext.ResidentFamilyMembers.Where(m => m.ResidentId == resident.ResidentId);
        _dbContext.ResidentFamilyMembers.RemoveRange(existingMembers);

        foreach (var memberRequest in request.FamilyMembers)
        {
            var member = new ResidentFamilyMember
            {
                FamilyMemberId = Guid.NewGuid(),
                RtId = rtId,
                ResidentId = resident.ResidentId,
                FullName = memberRequest.FullName,
                BirthDate = memberRequest.BirthDate,
                Gender = memberRequest.Gender,
                Relationship = memberRequest.Relationship,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.ResidentFamilyMembers.Add(member);
        }

        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "ResidentProfileSubmitted", new
        {
            resident.ResidentId,
            resident.FullName,
            resident.NationalIdNumber
        }, user.UserId, cancellationToken);

        foreach (var member in request.FamilyMembers)
        {
            await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "FamilyMemberUpserted", new
            {
                member.FullName,
                member.Relationship
            }, user.UserId, cancellationToken);
        }

        return Accepted(new { resident.ResidentId, resident.ApprovalStatus });
    }

    [HttpPost("{residentId:guid}/approval")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ReviewResidentAsync(Guid residentId, [FromBody] ApproveResidentRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var resident = await _dbContext.Residents.FirstOrDefaultAsync(r => r.ResidentId == residentId && r.RtId == rtId, cancellationToken);
        if (resident is null)
        {
            return NotFound();
        }

        resident.ApprovalStatus = request.Approve ? "APPROVED" : "REJECTED";
        resident.ApprovalNote = request.Approve ? null : request.Note;
        resident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId,
            request.Approve ? "ResidentProfileApproved" : "ResidentProfileRejected",
            new { resident.ResidentId, request.Note }, userId, cancellationToken);

        return Ok(new { resident.ResidentId, resident.ApprovalStatus, resident.ApprovalNote });
    }
}
