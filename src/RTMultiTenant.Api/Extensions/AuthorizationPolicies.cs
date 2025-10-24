namespace RTMultiTenant.Api.Extensions;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "RequireAdmin";
    public const string ResidentOnly = "RequireResident";
}
