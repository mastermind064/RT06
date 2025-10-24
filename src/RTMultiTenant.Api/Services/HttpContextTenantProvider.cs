using System.Security.Claims;

namespace RTMultiTenant.Api.Services;

public interface ITenantProvider
{
    Guid GetRtId();
    Guid GetUserId();
    Guid? GetResidentId();
    bool IsAdmin();
}

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRtId()
    {
        var claim = GetClaim("rt_id");
        return Guid.Parse(claim ?? throw new UnauthorizedAccessException("Missing tenant context"));
    }

    public Guid GetUserId()
    {
        var claim = GetClaim(ClaimTypes.NameIdentifier) ?? GetClaim("sub");
        return Guid.Parse(claim ?? throw new UnauthorizedAccessException("Missing user context"));
    }

    public Guid? GetResidentId()
    {
        var claim = GetClaim("resident_id");
        return claim is null ? null : Guid.Parse(claim);
    }

    public bool IsAdmin()
    {
        var role = GetClaim("role");
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private string? GetClaim(string type)
    {
        return _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == type)?.Value;
    }
}
