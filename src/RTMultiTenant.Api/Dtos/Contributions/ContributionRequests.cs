namespace RTMultiTenant.Api.Dtos.Contributions;

public class ContributionReportRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ProofImagePath { get; set; } = default!;
}

public class ContributionUpdateRequest
{
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string ProofImagePath { get; set; } = default!;
}

public class ContributionReviewRequest
{
    public bool Approve { get; set; }
    public string? AdminNote { get; set; }
}
