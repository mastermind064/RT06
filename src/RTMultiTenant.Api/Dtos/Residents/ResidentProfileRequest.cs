using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTMultiTenant.Api.Dtos.Residents;

public class ResidentProfileRequest
{
    public string NationalIdNumber { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = default!;
    public string Blok { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public IFormFile? KkDocumentPath { get; set; }
    public IFormFile? PicPath { get; set; }
    //public List<ResidentFamilyMemberRequest> FamilyMembers { get; set; } = new();
    public bool KkDelete { get; set; }
    public bool PicDelete { get; set; }
    [FromForm(Name = "familyMembers")]
    public string? FamilyMembersJson { get; set; }

    // 👉 property ini akan diisi manual oleh controller
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
