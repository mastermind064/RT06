using FluentValidation;
using RTMultiTenant.Api.Dtos.Rt;

namespace RTMultiTenant.Api.Validators;

public class CreateRtRequestValidator : AbstractValidator<CreateRtRequest>
{
    public CreateRtRequestValidator()
    {
        RuleFor(x => x.RtNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.RwNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.VillageName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SubdistrictName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CityName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ProvinceName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminUser).SetValidator(new CreateAdminUserRequestValidator());
    }
}

public class CreateAdminUserRequestValidator : AbstractValidator<CreateAdminUserRequest>
{
    public CreateAdminUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
