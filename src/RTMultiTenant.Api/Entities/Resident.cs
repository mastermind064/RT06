using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class Resident
{
    [Key]
    public Guid ResidentId { get; set; }
    public Guid RtId { get; set; }
    public string NationalIdNumber { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? KkDocumentPath { get; set; }
    public string ApprovalStatus { get; set; } = "DRAFT";
    public string? ApprovalNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
    public ICollection<ResidentFamilyMember> FamilyMembers { get; set; } = new HashSet<ResidentFamilyMember>();
    public ICollection<Contribution> Contributions { get; set; } = new HashSet<Contribution>();
}
