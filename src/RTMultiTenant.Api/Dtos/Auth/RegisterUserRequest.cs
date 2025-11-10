namespace RTMultiTenant.Api.Dtos.Auth;

public class RegisterUserRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Email { get; set; }
    public string Role { get; set; } = "WARGA";
    public Guid? ResidentId { get; set; }
}
