using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/cash/summary")]
[Authorize]
public class CashSummaryController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public CashSummaryController(AppDbContext dbContext, ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummaryAsync([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var query = _dbContext.MonthlyCashSummaries.Where(s => s.RtId == rtId);

        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year.Value);
        }

        if (month.HasValue)
        {
            if (!year.HasValue)
            {
                return BadRequest("Year must be provided when filtering by month");
            }
            query = query.Where(s => s.Month == month.Value);
        }

        var summaries = await query
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .Select(s => new
            {
                s.Year,
                s.Month,
                s.TotalContributionIn,
                s.TotalExpenseOut,
                s.BalanceEnd,
                s.GeneratedAt
            }).ToListAsync(cancellationToken);

        return Ok(summaries);
    }
}
