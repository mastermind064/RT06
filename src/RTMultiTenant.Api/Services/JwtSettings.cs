namespace RTMultiTenant.Api.Services;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    public const string DefaultSecret = "CHANGE_ME_SUPER_SECRET_KEY_1234567890";

    public string? Secret { get; set; } = DefaultSecret;
    public string Issuer { get; set; } = "RTMultiTenant";
    public string Audience { get; set; } = "RTMultiTenantUsers";
    public int ExpirationMinutes { get; set; } = 120;
}
