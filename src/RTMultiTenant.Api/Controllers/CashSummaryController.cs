using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Services;
using RTMultiTenant.Api.Extensions;

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

    // }

    [HttpGet("monitoring")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetMonitoringAsync(
        [FromQuery] int year,
        [FromQuery] string? name,
        [FromQuery] string? blok,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (year <= 0) return BadRequest("year is required");

        var rtId = _tenantProvider.GetRtId();

        var residents = _dbContext.Residents.AsQueryable().Where(r => r.RtId == rtId);
        if (!string.IsNullOrWhiteSpace(name)) residents = residents.Where(r => r.FullName.Contains(name));
        if (!string.IsNullOrWhiteSpace(blok)) residents = residents.Where(r => r.Blok.Contains(blok));

        var total = await residents.CountAsync(cancellationToken);

        var contrib = _dbContext.Contributions.Where(c => c.RtId == rtId && c.Status == "APPROVED" && c.PaymentDate.Year == year);

        var items = await residents
            .OrderBy(r => r.Blok).ThenBy(r => r.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.ResidentId,
                r.FullName,
                r.Blok,
                M1 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 1).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M2 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 2).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M3 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 3).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M4 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 4).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M5 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 5).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M6 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 6).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M7 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 7).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M8 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 8).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M9 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 9).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M10 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 10).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M11 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 11).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
                M12 = contrib.Where(c => c.ResidentId == r.ResidentId && c.PaymentDate.Month == 12).Sum(c => (decimal?)c.AmountPaid) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var itemsWithTotal = items.Select(x => new
        {
            x.ResidentId,
            x.FullName,
            x.Blok,
            x.M1,
            x.M2,
            x.M3,
            x.M4,
            x.M5,
            x.M6,
            x.M7,
            x.M8,
            x.M9,
            x.M10,
            x.M11,
            x.M12,
            Total = x.M1 + x.M2 + x.M3 + x.M4 + x.M5 + x.M6 + x.M7 + x.M8 + x.M9 + x.M10 + x.M11 + x.M12
        }).ToList();

        // footer totals across all filtered residents (not paged)
        var residentIds = residents.Select(r => r.ResidentId);
        var contribFiltered = contrib.Where(c => residentIds.Contains(c.ResidentId));

        var footer = await contribFiltered.GroupBy(_ => 1).Select(g => new
        {
            M1 = g.Where(c => c.PaymentDate.Month == 1).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M2 = g.Where(c => c.PaymentDate.Month == 2).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M3 = g.Where(c => c.PaymentDate.Month == 3).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M4 = g.Where(c => c.PaymentDate.Month == 4).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M5 = g.Where(c => c.PaymentDate.Month == 5).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M6 = g.Where(c => c.PaymentDate.Month == 6).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M7 = g.Where(c => c.PaymentDate.Month == 7).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M8 = g.Where(c => c.PaymentDate.Month == 8).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M9 = g.Where(c => c.PaymentDate.Month == 9).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M10 = g.Where(c => c.PaymentDate.Month == 10).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M11 = g.Where(c => c.PaymentDate.Month == 11).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
            M12 = g.Where(c => c.PaymentDate.Month == 12).Sum(c => (decimal?)c.AmountPaid) ?? 0m,
        }).FirstOrDefaultAsync(cancellationToken);

        footer ??= new { M1 = 0m, M2 = 0m, M3 = 0m, M4 = 0m, M5 = 0m, M6 = 0m, M7 = 0m, M8 = 0m, M9 = 0m, M10 = 0m, M11 = 0m, M12 = 0m };
        var footerWithTotal = new
        {
            footer.M1,
            footer.M2,
            footer.M3,
            footer.M4,
            footer.M5,
            footer.M6,
            footer.M7,
            footer.M8,
            footer.M9,
            footer.M10,
            footer.M11,
            footer.M12,
            Total = footer.M1 + footer.M2 + footer.M3 + footer.M4 + footer.M5 + footer.M6 + footer.M7 + footer.M8 + footer.M9 + footer.M10 + footer.M11 + footer.M12
        };

        return Ok(new { Items = itemsWithTotal, Total = total, Footer = footerWithTotal });
    }
}
