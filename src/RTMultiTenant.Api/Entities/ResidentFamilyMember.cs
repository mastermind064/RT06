using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class ResidentFamilyMember
{
    [Key]
    public Guid FamilyMemberId { get; set; }
    public Guid RtId { get; set; }
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = default!;
    public string Relationship { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(ResidentId))]
    public Resident Resident { get; set; } = default!;
    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
}
