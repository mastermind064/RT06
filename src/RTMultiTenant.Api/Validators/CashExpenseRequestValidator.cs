using FluentValidation;
using RTMultiTenant.Api.Dtos.Cash;

namespace RTMultiTenant.Api.Validators;

public class CashExpenseRequestValidator : AbstractValidator<CashExpenseRequest>
{
    public CashExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class CashExpenseUpdateRequestValidator : AbstractValidator<CashExpenseUpdateRequest>
{
    public CashExpenseUpdateRequestValidator()
    {
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
