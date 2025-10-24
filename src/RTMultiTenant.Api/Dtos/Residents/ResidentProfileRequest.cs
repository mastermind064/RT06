namespace RTMultiTenant.Api.Dtos.Residents;

public class ResidentProfileRequest
{
    public string NationalIdNumber { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? KkDocumentPath { get; set; }
    public List<ResidentFamilyMemberRequest> FamilyMembers { get; set; } = new();
}

public class ResidentFamilyMemberRequest
{
    public string FullName { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = default!;
    public string Relationship { get; set; } = default!;
}

public class ApproveResidentRequest
{
    public bool Approve { get; set; }
    public string? Note { get; set; }
}
