namespace RTMultiTenant.Api.Dtos.Rt;

public class CreateRtRequest
{
    public string RtNumber { get; set; } = default!;
    public string RwNumber { get; set; } = default!;
    public string VillageName { get; set; } = default!;
    public string SubdistrictName { get; set; } = default!;
    public string CityName { get; set; } = default!;
    public string ProvinceName { get; set; } = default!;
    public string? AddressDetail { get; set; }

    public CreateAdminUserRequest AdminUser { get; set; } = new();
}

public class CreateAdminUserRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}
