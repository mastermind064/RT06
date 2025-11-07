using FluentValidation;
using RTMultiTenant.Api.Dtos.Residents;

namespace RTMultiTenant.Api.Validators;

public class ResidentProfileRequestValidator : AbstractValidator<ResidentProfileRequest>
{
    public ResidentProfileRequestValidator()
    {
        RuleFor(x => x.NationalIdNumber).NotEmpty().Length(8, 32);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Gender).NotEmpty().Must(g => g is "L" or "P");
        RuleFor(x => x.Blok).NotEmpty().MaximumLength(3);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleForEach(x => x.FamilyMembers).SetValidator(new ResidentFamilyMemberRequestValidator());
    }
}

public class ResidentFamilyMemberRequestValidator : AbstractValidator<ResidentFamilyMemberRequest>
{
    public ResidentFamilyMemberRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Gender).NotEmpty().Must(g => g is "L" or "P");
        RuleFor(x => x.Relationship).NotEmpty();
    }
}
