using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class User
{
    [Key]
    public Guid UserId { get; set; }
    public Guid RtId { get; set; }
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string? Email { get; set; }
    public string Role { get; set; } = "WARGA";
    public bool IsActive { get; set; } = true;
    public Guid? ResidentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
    [ForeignKey(nameof(ResidentId))]
    public Resident? Resident { get; set; }
    public ICollection<CashExpense> CashExpenses { get; set; } = new HashSet<CashExpense>();
}
