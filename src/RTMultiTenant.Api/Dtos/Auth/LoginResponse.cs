namespace RTMultiTenant.Api.Dtos.Auth;

public class LoginResponse
{
    public string Token { get; set; } = default!;
    public Guid RtId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
}
