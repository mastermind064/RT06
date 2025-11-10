namespace RTMultiTenant.Api.Dtos.Auth;

public class ForgotPasswordRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Blok { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
