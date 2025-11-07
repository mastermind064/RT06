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
    public async Task<IActionResult> GetSummaryAsync(
    [FromQuery] int? year,
    [FromQuery] int? month,
    CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();

        // base query
        var query = _dbContext.MonthlyCashSummaries.Where(s => s.RtId == rtId);
        var yearlyQuery = _dbContext.MonthlyCashSummaries.Where(s => s.RtId == rtId);

        if (year.HasValue)
        {
            query = query.Where(s => s.Year == year.Value);
            yearlyQuery = yearlyQuery.Where(s => s.Year == year.Value);
        }

        if (month.HasValue)
        {
            if (!year.HasValue)
                return BadRequest("Year must be provided when filtering by month");

            query = query.Where(s => s.Month == month.Value);
        }

        // YEARLY
        var yearlySummary = await yearlyQuery
            .GroupBy(s => s.Year)
            .Select(g => new
            {
                Year = g.Key,
                TotalContributionIn = g.Sum(s => s.TotalContributionIn),
                TotalExpenseOut = g.Sum(s => s.TotalExpenseOut),
                BalanceEnd = g
                    .OrderByDescending(s => s.Month)
                    .Select(x => x.BalanceEnd)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken); // hanya 1 year

        // fallback tahunan jika kosong juga
        yearlySummary ??= new
        {
            Year = year ?? DateTime.UtcNow.Year,
            TotalContributionIn = 0m,
            TotalExpenseOut = 0m,
            BalanceEnd = 0m
        };

        // MONTHLY (ambil satu, kalau tidak ada => null)
        var monthlySummary = await query
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
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (monthlySummary == null)
        {
            monthlySummary = new
            {
                Year = year ?? DateTime.UtcNow.Year,
                Month = month ?? DateTime.UtcNow.Month,
                TotalContributionIn = 0m,
                TotalExpenseOut = 0m,
                BalanceEnd = 0m,
                GeneratedAt = default(DateTime)
            };
        }

        // kalau benar-benar tidak ada data sama sekali
        if (monthlySummary == null && yearlySummary == null)
        {
            return NotFound("Summary data not found");
        }

        // bentuk response yang jelas
        var result = new
        {
            Monthly = monthlySummary, // bisa null kalau bulan tsb belum ada
            Yearly = yearlySummary
        };

        return Ok(result);
    }

}
