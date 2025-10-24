using System.ComponentModel.DataAnnotations;

namespace RTMultiTenant.Api.Entities;

public class Rt
{
    [Key]
    public Guid RtId { get; set; }
    public string RtNumber { get; set; } = default!;
    public string RwNumber { get; set; } = default!;
    public string VillageName { get; set; } = default!;
    public string SubdistrictName { get; set; } = default!;
    public string CityName { get; set; } = default!;
    public string ProvinceName { get; set; } = default!;
    public string? AddressDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new HashSet<User>();
}
