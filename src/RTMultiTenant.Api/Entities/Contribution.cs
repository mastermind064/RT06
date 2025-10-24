using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class Contribution
{
    [Key]
    public Guid ContributionId { get; set; }
    public Guid RtId { get; set; }
    public Guid ResidentId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ProofImagePath { get; set; } = default!;
    public string Status { get; set; } = "PENDING";
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(ResidentId))]
    public Resident Resident { get; set; } = default!;
    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
}
