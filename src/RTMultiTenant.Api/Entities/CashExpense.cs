using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class CashExpense
{
    [Key]
    public Guid ExpenseId { get; set; }
    public Guid RtId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedBy { get; set; } = default!;
    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
}
