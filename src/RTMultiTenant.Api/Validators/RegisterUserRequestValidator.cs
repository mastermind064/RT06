using FluentValidation;
using RTMultiTenant.Api.Dtos.Auth;

namespace RTMultiTenant.Api.Validators;

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).NotEmpty().Must(role => role is "ADMIN" or "WARGA");
    }
}
