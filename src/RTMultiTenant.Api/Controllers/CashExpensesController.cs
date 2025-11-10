using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Cash;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Extensions;
using RTMultiTenant.Api.Services;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/cash/expenses")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class CashExpensesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    private readonly EventPublisher _eventPublisher;
    private readonly MonthlySummaryUpdater _summaryUpdater;

    public CashExpensesController(AppDbContext dbContext, ITenantProvider tenantProvider, EventPublisher eventPublisher,
        MonthlySummaryUpdater summaryUpdater)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
        _eventPublisher = eventPublisher;
        _summaryUpdater = summaryUpdater;
    }

    [HttpGet("{expenseId:guid}")]
    public async Task<IActionResult> GetExpensesByIdAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var expenses = await _dbContext.CashExpenses
            .Where(e => e.RtId == rtId && e.ExpenseId == expenseId)
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new
            {
                e.ExpenseId,
                e.ExpenseDate,
                e.Description,
                e.Amount,
                e.IsActive
            }).ToListAsync(cancellationToken);

        return Ok(expenses);
    }

    [HttpGet]
    public async Task<IActionResult> GetExpensesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var rtId = _tenantProvider.GetRtId();
        var query = _dbContext.CashExpenses.Where(e => e.RtId == rtId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.ExpenseId,
                e.ExpenseDate,
                e.Description,
                e.Amount,
                e.IsActive
            }).ToListAsync(cancellationToken);

        return Ok(new { Items = items, Total = total });
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpenseAsync([FromBody] CashExpenseRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var now = DateTime.UtcNow;
        var expense = new CashExpense
        {
            ExpenseId = Guid.NewGuid(),
            RtId = rtId,
            ExpenseDate = request.ExpenseDate,
            Description = request.Description,
            Amount = request.Amount,
            CreatedByUserId = userId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.CashExpenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CASHFLOW", expense.ExpenseId, "ExpenseRecorded", new
        {
            expense.ExpenseId,
            expense.Amount,
            expense.ExpenseDate
        }, userId, cancellationToken);

        await _summaryUpdater.AdjustExpenseAsync(rtId, expense.ExpenseDate, expense.Amount, cancellationToken);

        return CreatedAtAction(nameof(GetExpensesAsync), new { expense.ExpenseId }, new
        {
            expense.ExpenseId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.IsActive
        });
    }

    [HttpPut("{expenseId:guid}")]
    public async Task<IActionResult> UpdateExpenseAsync(Guid expenseId, [FromBody] CashExpenseUpdateRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var expense = await _dbContext.CashExpenses.FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.RtId == rtId, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        var originalDate = expense.ExpenseDate;
        var originalAmount = expense.Amount;
        var originalStatus = expense.IsActive;

        expense.ExpenseDate = request.ExpenseDate;
        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.IsActive = request.IsActive;
        expense.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CASHFLOW", expense.ExpenseId, "ExpenseUpdated", new
        {
            expense.ExpenseId,
            expense.Amount,
            expense.ExpenseDate,
            expense.IsActive
        }, userId, cancellationToken);

        if (originalStatus)
        {
            await _summaryUpdater.AdjustExpenseAsync(rtId, originalDate, -originalAmount, cancellationToken);
        }

        if (expense.IsActive)
        {
            await _summaryUpdater.AdjustExpenseAsync(rtId, expense.ExpenseDate, expense.Amount, cancellationToken);
        }

        return Ok(new
        {
            expense.ExpenseId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.IsActive
        });
    }

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> DeleteExpenseAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var expense = await _dbContext.CashExpenses.FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.RtId == rtId, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        if (!expense.IsActive)
        {
            return NoContent();
        }

        expense.IsActive = false;
        expense.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("CASHFLOW", expense.ExpenseId, "ExpenseDeactivated", new
        {
            expense.ExpenseId
        }, userId, cancellationToken);

        await _summaryUpdater.AdjustExpenseAsync(rtId, expense.ExpenseDate, -expense.Amount, cancellationToken);

        return NoContent();
    }
}
