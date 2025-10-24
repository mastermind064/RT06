using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class MonthlyCashSummary
{
    [Key]
    public Guid SummaryId { get; set; }
    public Guid RtId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalContributionIn { get; set; }
    public decimal TotalExpenseOut { get; set; }
    public decimal BalanceEnd { get; set; }
    public DateTime GeneratedAt { get; set; }

    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
}
