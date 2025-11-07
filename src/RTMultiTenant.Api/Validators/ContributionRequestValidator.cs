using FluentValidation;
using RTMultiTenant.Api.Dtos.Contributions;

namespace RTMultiTenant.Api.Validators;

public class ContributionReportRequestValidator : AbstractValidator<ContributionReportRequest>
{
    public ContributionReportRequestValidator()
    {
        RuleFor(x => x.PeriodStart).NotEmpty();
        RuleFor(x => x.PeriodEnd).NotEmpty().GreaterThanOrEqualTo(x => x.PeriodStart);
        RuleFor(x => x.AmountPaid).GreaterThan(0);
        RuleFor(x => x.Proof).NotEmpty();
    }
}

public class ContributionUpdateRequestValidator : AbstractValidator<ContributionUpdateRequest>
{
    public ContributionUpdateRequestValidator()
    {
        RuleFor(x => x.AmountPaid).GreaterThan(0);
        RuleFor(x => x.Proof).NotEmpty();
    }
}
