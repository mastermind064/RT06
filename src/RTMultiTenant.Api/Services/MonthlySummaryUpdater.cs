using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Entities;

namespace RTMultiTenant.Api.Services;

public class MonthlySummaryUpdater
{
    private readonly AppDbContext _dbContext;

    public MonthlySummaryUpdater(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AdjustContributionAsync(Guid rtId, DateTime paymentDate, decimal amountDelta, CancellationToken ct)
    {
        var (year, month) = (paymentDate.Year, paymentDate.Month);
        var summary = await GetOrCreateSummary(rtId, year, month, ct);
        summary.TotalContributionIn += amountDelta;
        summary.GeneratedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        await RecalculateBalancesAsync(rtId, ct);
    }

    public async Task AdjustExpenseAsync(Guid rtId, DateTime expenseDate, decimal amountDelta, CancellationToken ct)
    {
        var (year, month) = (expenseDate.Year, expenseDate.Month);
        var summary = await GetOrCreateSummary(rtId, year, month, ct);
        summary.TotalExpenseOut += amountDelta;
        summary.GeneratedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        await RecalculateBalancesAsync(rtId, ct);
    }

    private async Task<MonthlyCashSummary> GetOrCreateSummary(Guid rtId, int year, int month, CancellationToken ct)
    {
        var summary = await _dbContext.MonthlyCashSummaries
            .FirstOrDefaultAsync(s => s.RtId == rtId && s.Year == year && s.Month == month, ct);

        if (summary != null)
        {
            return summary;
        }

        summary = new MonthlyCashSummary
        {
            SummaryId = Guid.NewGuid(),
            RtId = rtId,
            Year = year,
            Month = month,
            GeneratedAt = DateTime.UtcNow
        };

        _dbContext.MonthlyCashSummaries.Add(summary);
        await _dbContext.SaveChangesAsync(ct);
        return summary;
    }

    private async Task RecalculateBalancesAsync(Guid rtId, CancellationToken ct)
    {
        var summaries = await _dbContext.MonthlyCashSummaries
            .Where(s => s.RtId == rtId)
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToListAsync(ct);

        decimal runningBalance = 0m;
        foreach (var summary in summaries)
        {
            runningBalance += summary.TotalContributionIn - summary.TotalExpenseOut;
            summary.BalanceEnd = runningBalance;
            summary.GeneratedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
