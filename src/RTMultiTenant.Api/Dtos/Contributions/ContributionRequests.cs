namespace RTMultiTenant.Api.Dtos.Contributions;

public class ContributionReportRequest
{
    public string PeriodStart { get; set; } = default!;   // pakai string dulu, nanti parse ke DateTime
    public string PeriodEnd { get; set; } = default!;
    public decimal AmountPaid { get; set; }
    public string PaymentDate { get; set; } = default!;

    // nama harus sama dengan key yang kamu append di FormData: "proof"
    public IFormFile? Proof { get; set; }
}

public class ContributionUpdateRequest
{
    public string PeriodStart { get; set; } = default!;   // pakai string dulu, nanti parse ke DateTime
    public string PeriodEnd { get; set; } = default!;
    public decimal AmountPaid { get; set; }
    public string PaymentDate { get; set; } = default!;

    // nama harus sama dengan key yang kamu append di FormData: "proof"
    public IFormFile? Proof { get; set; }
}

public class ContributionReviewRequest
{
    public bool Approve { get; set; }
    public string? AdminNote { get; set; }
}
