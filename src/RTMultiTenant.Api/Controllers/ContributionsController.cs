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
    public async Task<IActionResult> GetMyContributionsAsync(
        [FromQuery] string? status,
        [FromQuery] string? blok,
        [FromQuery] string? adminNote,
        [FromQuery] DateTime? paymentDateFrom,
        [FromQuery] DateTime? paymentDateTo,
        [FromQuery] decimal? amountMin,
        [FromQuery] decimal? amountMax,
        [FromQuery] DateTime? periodStartFrom,
        [FromQuery] DateTime? periodStartTo,
        [FromQuery] DateTime? periodEndFrom,
        [FromQuery] DateTime? periodEndTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var rtId = _tenantProvider.GetRtId();
        var residentId = _tenantProvider.GetResidentId();
        if (!residentId.HasValue)
        {
            return Ok(Array.Empty<object>());
        }

        var query = _dbContext.Contributions
            .Where(c => c.RtId == rtId && c.ResidentId == residentId.Value);

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(blok)) query = query.Where(c => c.Resident.Blok.Contains(blok));
        if (!string.IsNullOrWhiteSpace(adminNote)) query = query.Where(c => c.AdminNote != null && c.AdminNote.Contains(adminNote));
        if (paymentDateFrom.HasValue) query = query.Where(c => c.PaymentDate >= paymentDateFrom.Value);
        if (paymentDateTo.HasValue) query = query.Where(c => c.PaymentDate <= paymentDateTo.Value);
        if (amountMin.HasValue) query = query.Where(c => c.AmountPaid >= amountMin.Value);
        if (amountMax.HasValue) query = query.Where(c => c.AmountPaid <= amountMax.Value);
        if (periodStartFrom.HasValue) query = query.Where(c => c.PeriodStart >= periodStartFrom.Value);
        if (periodStartTo.HasValue) query = query.Where(c => c.PeriodStart <= periodStartTo.Value);
        if (periodEndFrom.HasValue) query = query.Where(c => c.PeriodEnd >= periodEndFrom.Value);
        if (periodEndTo.HasValue) query = query.Where(c => c.PeriodEnd <= periodEndTo.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.ContributionId,
                c.PeriodStart,
                c.PeriodEnd,
                c.AmountPaid,
                c.PaymentDate,
                c.Status,
                c.AdminNote,
                Blok = c.Resident.Blok,
                c.ProofImagePath
            }).ToListAsync(cancellationToken);

        return Ok(new { Items = items, Total = total });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetByRtAsync(
        [FromQuery] string? status,
        [FromQuery] string? blok,
        [FromQuery] string? adminNote,
        [FromQuery] DateTime? paymentDateFrom,
        [FromQuery] DateTime? paymentDateTo,
        [FromQuery] decimal? amountMin,
        [FromQuery] decimal? amountMax,
        [FromQuery] DateTime? periodStartFrom,
        [FromQuery] DateTime? periodStartTo,
        [FromQuery] DateTime? periodEndFrom,
        [FromQuery] DateTime? periodEndTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var rtId = _tenantProvider.GetRtId();
        var query = _dbContext.Contributions.Where(c => c.RtId == rtId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(blok)) query = query.Where(c => c.Resident.Blok.Contains(blok));
        if (!string.IsNullOrWhiteSpace(adminNote)) query = query.Where(c => c.AdminNote != null && c.AdminNote.Contains(adminNote));
        if (paymentDateFrom.HasValue) query = query.Where(c => c.PaymentDate >= paymentDateFrom.Value);
        if (paymentDateTo.HasValue) query = query.Where(c => c.PaymentDate <= paymentDateTo.Value);
        if (amountMin.HasValue) query = query.Where(c => c.AmountPaid >= amountMin.Value);
        if (amountMax.HasValue) query = query.Where(c => c.AmountPaid <= amountMax.Value);
        if (periodStartFrom.HasValue) query = query.Where(c => c.PeriodStart >= periodStartFrom.Value);
        if (periodStartTo.HasValue) query = query.Where(c => c.PeriodStart <= periodStartTo.Value);
        if (periodEndFrom.HasValue) query = query.Where(c => c.PeriodEnd >= periodEndFrom.Value);
        if (periodEndTo.HasValue) query = query.Where(c => c.PeriodEnd <= periodEndTo.Value);

        var total = await query.CountAsync(cancellationToken);

        var results = await query
            .OrderByDescending(c => c.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.ContributionId,
                c.ResidentId,
                c.AmountPaid,
                c.PaymentDate,
                c.Status,
                c.AdminNote,
                c.PeriodStart,
                c.PeriodEnd,
                Blok = c.Resident.Blok,
                c.ProofImagePath
            }).ToListAsync(cancellationToken);

        return Ok(new { Items = results, Total = total });
    }

    [HttpGet("{contributionId:guid}/edit")]
    [Authorize]
    public async Task<IActionResult> GetByUseerAsync(Guid contributionId, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var query = _dbContext.Contributions.Where(c => c.RtId == rtId);
        if (contributionId != null)
        {
            query = query.Where(c => c.ContributionId == contributionId);
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
                c.AdminNote,
                c.PeriodStart,
                c.PeriodEnd,
                Blok = c.Resident.Blok,
                c.ProofImagePath
            }).FirstOrDefaultAsync(cancellationToken);

        return Ok(results);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> ReportContributionAsync([FromForm] ContributionReportRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var residentId = _tenantProvider.GetResidentId();
        if (!residentId.HasValue)
        {
            return BadRequest("Resident profile is required before reporting contributions");
        }

        // 1. Parse periodStart & periodEnd (contoh format '2025-01' = Januari 2025)
        // kamu bisa sesuaikan logikanya
        if (!DateTime.TryParse(request.PeriodStart + "-01", out var periodStartDate))
        {
            return BadRequest("Invalid periodStart format");
        }

        if (!DateTime.TryParse(request.PeriodEnd + "-01", out var periodEndDate))
        {
            return BadRequest("Invalid periodEnd format");
        }

        if (!DateTime.TryParse(request.PaymentDate, out var paymentDate))
        {
            return BadRequest("Invalid paymentDate format");
        }

        // 2. Simpan file bukti bayar (kalau ada)
        string proofImagePath = string.Empty;

        if (request.Proof is not null && request.Proof.Length > 0)
        {
            // tentukan folder upload, misal wwwroot/uploads/contributions/{rtId}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contributions", rtId.ToString());
            Directory.CreateDirectory(root);

            var fileName = $"{Guid.NewGuid()}_{request.Proof.FileName}";
            var fullPath = Path.Combine(root, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.Proof.CopyToAsync(stream, cancellationToken);
            }

            // simpan path relatif yg bisa ditampilkan lagi ke FE
            proofImagePath = $"/uploads/contributions/{rtId}/{fileName}";
        }

        var now = DateTime.UtcNow;
        var contribution = new Contribution
        {
            ContributionId = Guid.NewGuid(),
            RtId = rtId,
            ResidentId = residentId.Value,
            PeriodStart = periodStartDate,
            PeriodEnd = periodEndDate,
            AmountPaid = request.AmountPaid,
            PaymentDate = paymentDate,
            ProofImagePath = proofImagePath,
            Status = "PENDING",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Contributions.Add(contribution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync(
            "CONTRIBUTION",
            contribution.ContributionId,
            "ContributionReported",
            new
            {
                contribution.ContributionId,
                contribution.AmountPaid,
                contribution.PaymentDate
            },
            userId,
            cancellationToken
        );

        return Accepted(new
        {
            contribution.ContributionId,
            contribution.Status
        });
    }

    [HttpPut("{contributionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> UpdateContributionAsync(Guid contributionId, [FromForm] ContributionUpdateRequest request,
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

        if (contribution.Status != "PENDING" && contribution.Status != "REJECTED")
        {
            return BadRequest("Only pending or rejected contributions can be updated");
        }

        // 1. Parse periodStart & periodEnd (contoh format '2025-01' = Januari 2025)
        // kamu bisa sesuaikan logikanya
        if (!DateTime.TryParse(request.PeriodStart + "-01", out var periodStartDate))
        {
            return BadRequest("Invalid periodStart format");
        }

        if (!DateTime.TryParse(request.PeriodEnd + "-01", out var periodEndDate))
        {
            return BadRequest("Invalid periodEnd format");
        }

        if (!DateTime.TryParse(request.PaymentDate, out var paymentDate))
        {
            return BadRequest("Invalid paymentDate format");
        }

        // 2. Simpan file bukti bayar (kalau ada)
        string proofImagePath = string.Empty;

        if (request.Proof is not null && request.Proof.Length > 0)
        {
            // tentukan folder upload, misal wwwroot/uploads/contributions/{rtId}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contributions", rtId.ToString());
            Directory.CreateDirectory(root);

            var fileName = $"{Guid.NewGuid()}_{request.Proof.FileName}";
            var fullPath = Path.Combine(root, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.Proof.CopyToAsync(stream, cancellationToken);
            }

            // simpan path relatif yg bisa ditampilkan lagi ke FE
            proofImagePath = $"/uploads/contributions/{rtId}/{fileName}";
        }

        contribution.PeriodStart = periodStartDate;
        contribution.PeriodEnd = periodEndDate;
        contribution.AmountPaid = request.AmountPaid;
        contribution.PaymentDate = paymentDate;
        contribution.ProofImagePath = proofImagePath;
        // Reset to PENDING so it can be reviewed again if previously REJECTED
        contribution.Status = "PENDING";
        contribution.AdminNote = null;
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

    [HttpPost("{contributionId:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> RejectContributionAsync(Guid contributionId, [FromBody] ContributionRejectRequest request,
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

        contribution.Status = "REJECTED";
        contribution.AdminNote = request.Note;
        contribution.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CONTRIBUTION", contribution.ContributionId,
            "ContributionRejected",
            new { contribution.ContributionId, request.Note }, userId, cancellationToken);

        return Ok(new { contribution.ContributionId, contribution.Status, contribution.AdminNote });
    }
}
