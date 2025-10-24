using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Contributions;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Extensions;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/contributions")]
public class ContributionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    private readonly EventPublisher _eventPublisher;
    private readonly MonthlySummaryUpdater _summaryUpdater;

    public ContributionsController(AppDbContext dbContext, ITenantProvider tenantProvider, EventPublisher eventPublisher,
        MonthlySummaryUpdater summaryUpdater)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
        _eventPublisher = eventPublisher;
        _summaryUpdater = summaryUpdater;
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> GetMyContributionsAsync(CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var residentId = _tenantProvider.GetResidentId();
        if (!residentId.HasValue)
        {
            return Ok(Array.Empty<object>());
        }

        var contributions = await _dbContext.Contributions
            .Where(c => c.RtId == rtId && c.ResidentId == residentId.Value)
            .OrderByDescending(c => c.PaymentDate)
            .Select(c => new
            {
                c.ContributionId,
                c.PeriodStart,
                c.PeriodEnd,
                c.AmountPaid,
                c.PaymentDate,
                c.Status,
                c.AdminNote,
                c.ProofImagePath
            }).ToListAsync(cancellationToken);

        return Ok(contributions);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetByRtAsync([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var query = _dbContext.Contributions.Where(c => c.RtId == rtId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var results = await query
            .OrderByDescending(c => c.PaymentDate)
            .Select(c => new
            {
                c.ContributionId,
                c.ResidentId,
                c.AmountPaid,
                c.PaymentDate,
                c.Status,
                c.AdminNote
            }).ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> ReportContributionAsync([FromBody] ContributionReportRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var residentId = _tenantProvider.GetResidentId();
        if (!residentId.HasValue)
        {
            return BadRequest("Resident profile is required before reporting contributions");
        }

        var now = DateTime.UtcNow;
        var contribution = new Contribution
        {
            ContributionId = Guid.NewGuid(),
            RtId = rtId,
            ResidentId = residentId.Value,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            AmountPaid = request.AmountPaid,
            PaymentDate = request.PaymentDate,
            ProofImagePath = request.ProofImagePath,
            Status = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Contributions.Add(contribution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CONTRIBUTION", contribution.ContributionId, "ContributionReported", new
        {
            contribution.ContributionId,
            contribution.AmountPaid,
            contribution.PaymentDate
        }, userId, cancellationToken);

        return Accepted(new { contribution.ContributionId, contribution.Status });
    }

    [HttpPut("{contributionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> UpdateContributionAsync(Guid contributionId, [FromBody] ContributionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var residentId = _tenantProvider.GetResidentId();
        if (!residentId.HasValue)
        {
            return Unauthorized();
        }

        var contribution = await _dbContext.Contributions.FirstOrDefaultAsync(c =>
            c.ContributionId == contributionId && c.RtId == rtId && c.ResidentId == residentId.Value, cancellationToken);

        if (contribution is null)
        {
            return NotFound();
        }

        if (contribution.Status != "PENDING")
        {
            return BadRequest("Only pending contributions can be updated");
        }

        contribution.AmountPaid = request.AmountPaid;
        contribution.PaymentDate = request.PaymentDate;
        contribution.ProofImagePath = request.ProofImagePath;
        contribution.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CONTRIBUTION", contribution.ContributionId, "ContributionUpdated", new
        {
            contribution.ContributionId,
            contribution.AmountPaid,
            contribution.PaymentDate
        }, userId, cancellationToken);

        return Ok(new { contribution.ContributionId, contribution.Status });
    }

    [HttpPost("{contributionId:guid}/review")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ReviewContributionAsync(Guid contributionId, [FromBody] ContributionReviewRequest request,
        CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var contribution = await _dbContext.Contributions.FirstOrDefaultAsync(c =>
            c.ContributionId == contributionId && c.RtId == rtId, cancellationToken);

        if (contribution is null)
        {
            return NotFound();
        }

        if (contribution.Status != "PENDING")
        {
            return BadRequest("Contribution already reviewed");
        }

        contribution.Status = request.Approve ? "APPROVED" : "REJECTED";
        contribution.AdminNote = request.AdminNote;
        contribution.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CONTRIBUTION", contribution.ContributionId,
            request.Approve ? "ContributionApproved" : "ContributionRejected",
            new { contribution.ContributionId, request.AdminNote }, userId, cancellationToken);

        if (request.Approve)
        {
            await _summaryUpdater.AdjustContributionAsync(rtId, contribution.PaymentDate, contribution.AmountPaid, cancellationToken);
        }

        return Ok(new { contribution.ContributionId, contribution.Status, contribution.AdminNote });
    }
}
