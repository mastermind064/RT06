using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Dtos.Residents;
using RTMultiTenant.Api.Entities;
using RTMultiTenant.Api.Extensions;
using RTMultiTenant.Api.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RTMultiTenant.Api.Controllers;

[ApiController]
[Route("api/residents")]
public class ResidentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    private readonly EventPublisher _eventPublisher;

    public ResidentsController(AppDbContext dbContext, ITenantProvider tenantProvider, EventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
        _eventPublisher = eventPublisher;
    }

    [HttpGet("{residentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetByIdAsync(Guid residentId, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var resident = await _dbContext.Residents
            .Include(r => r.FamilyMembers)
            .FirstOrDefaultAsync(r => r.ResidentId == residentId && r.RtId == rtId, cancellationToken);

        if (resident is null)
        {
            return NotFound();
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.RtId == rtId && u.ResidentId == resident.ResidentId, cancellationToken);

        return Ok(new
        {
            resident.ResidentId,
            resident.NationalIdNumber,
            resident.FullName,
            resident.BirthDate,
            resident.Gender,
            resident.Blok,
            resident.PhoneNumber,
            Email = user != null ? user.Email : null,
            resident.KkDocumentPath,
            resident.PicPath,
            resident.ApprovalStatus,
            resident.ApprovalNote,
            FamilyMembers = resident.FamilyMembers.Select(member => new
            {
                member.FamilyMemberId,
                member.FullName,
                member.BirthDate,
                member.Gender,
                member.Relationship
            })
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? name,
        [FromQuery] string? blok,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var rtId = _tenantProvider.GetRtId();
        var q = _dbContext.Residents.Where(r => r.RtId == rtId);

        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(r => r.FullName.Contains(name));
        if (!string.IsNullOrWhiteSpace(blok))
            q = q.Where(r => r.Blok.Contains(blok));
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(r => r.ApprovalStatus == status);

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(r => r.Blok)
            .ThenBy(r => r.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.ResidentId,
                r.FullName,
                r.Blok,
                r.PhoneNumber,
                r.ApprovalStatus
            }).ToListAsync(cancellationToken);

        return Ok(new { Items = items, Total = total });
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();
        var user = await _dbContext.Users.Include(u => u.Resident).ThenInclude(r => r!.FamilyMembers)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);

        if (user?.Resident is null)
        {
            return Ok(new { Resident = (Resident?)null });
        }

        var resident = user.Resident;
        return Ok(new
        {
            resident.ResidentId,
            resident.NationalIdNumber,
            resident.FullName,
            resident.BirthDate,
            resident.Gender,
            resident.Blok,
            resident.PhoneNumber,
            Email = user.Email,
            resident.KkDocumentPath,
            resident.PicPath,
            resident.ApprovalStatus,
            resident.ApprovalNote,
            FamilyMembers = resident.FamilyMembers.Select(member => new
            {
                member.FamilyMemberId,
                member.FullName,
                member.BirthDate,
                member.Gender,
                member.Relationship
            })
        });
    }

    [HttpPost("me")]
    [Authorize(Policy = AuthorizationPolicies.ResidentOnly)]
    public async Task<IActionResult> SubmitMyProfileAsync(
        [FromForm] ResidentProfileRequest request,
        [FromServices] IValidator<ResidentProfileRequest> validator, 
        CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        // 🧠 Deserialize family members sebelum validasi
        if (!string.IsNullOrWhiteSpace(request.FamilyMembersJson))
        {
            try
            {
                var jsonOpts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // ⬅️ penting
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                request.FamilyMembers = JsonSerializer
                    .Deserialize<List<ResidentFamilyMemberRequest>>(request.FamilyMembersJson!, jsonOpts) ?? new();
            }
            catch (Exception ex)
            {
                return BadRequest($"Format familyMembers tidak valid: {ex.Message}");
            }
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var user = await _dbContext.Users.Include(u => u.Resident).ThenInclude(r => r!.FamilyMembers)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        // ===== Helpers: path & file ops =====
        string WebRoot() => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        static string EnsureDir(string path) { Directory.CreateDirectory(path); return path; }

        string SaveFolder(string subfolder) =>
            EnsureDir(Path.Combine(WebRoot(), "uploads", "resident", subfolder, rtId.ToString()));

        static async Task<string> SaveFileAsync(IFormFile file, string folderAbs, string relPrefix, CancellationToken ct)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(folderAbs, fileName);
            await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, ct);
            }
            return $"{relPrefix}/{fileName}";
        }

        void DeleteFileIfExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            // normalize leading slash
            var rel = relativePath!.TrimStart(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var abs = Path.Combine(WebRoot(), rel.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(abs))
            {
                System.IO.File.Delete(abs);
            }
        }

        // Track file baru agar bisa dihapus kalau transaksi gagal
        var newFilesToCleanup = new List<string>(); // relative paths

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // ====== 1) Siapkan entity ======
            var now = DateTime.UtcNow;
            var resident = user.Resident ?? new Resident
            {
                ResidentId = Guid.NewGuid(),
                RtId = rtId,
                CreatedAt = now
            };

            // Default: pertahankan path lama jika tidak ada upload baru
            string kkDocumentPath = resident.KkDocumentPath ?? string.Empty;
            string picPath = resident.PicPath ?? string.Empty;

            // ====== 2) Hapus file lama (jika ada), lalu simpan file baru ======
            if (request.KkDocumentPath is not null && request.KkDocumentPath.Length > 0)
            {
                // Hapus lama → simpan baru
                DeleteFileIfExists(kkDocumentPath);

                var kkFolderAbs = SaveFolder("kk");
                var kkRelPrefix = $"/uploads/resident/kk/{rtId}";
                kkDocumentPath = await SaveFileAsync(request.KkDocumentPath, kkFolderAbs, kkRelPrefix, cancellationToken);
                newFilesToCleanup.Add(kkDocumentPath);
            }

            if (request.PicPath is not null && request.PicPath.Length > 0)
            {
                // Hapus lama → simpan baru
                DeleteFileIfExists(picPath);

                var picFolderAbs = SaveFolder("pic");
                var picRelPrefix = $"/uploads/resident/pic/{rtId}";
                picPath = await SaveFileAsync(request.PicPath, picFolderAbs, picRelPrefix, cancellationToken);
                newFilesToCleanup.Add(picPath);
            }

            // ====== 3) Validasi unik blok & email ======
            bool blokUsed = await _dbContext.Residents.AnyAsync(r => r.RtId == rtId && r.Blok == request.Blok && r.ResidentId != resident.ResidentId, cancellationToken);
            if (blokUsed)
            {
                return Conflict("Blok sudah digunakan warga lain");
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                bool emailUsed = await _dbContext.Users.AnyAsync(u => u.RtId == rtId && u.Email == request.Email && u.UserId != user.UserId, cancellationToken);
                if (emailUsed)
                {
                    return Conflict("Email sudah terdaftar");
                }
                user.Email = request.Email;
            }

            // ====== 4) Map field profile ======
            resident.NationalIdNumber = request.NationalIdNumber;
            resident.FullName = request.FullName;
            resident.BirthDate = request.BirthDate;
            resident.Gender = request.Gender;
            resident.Blok = request.Blok;
            resident.PhoneNumber = request.PhoneNumber;
            resident.KkDocumentPath = kkDocumentPath;
            resident.PicPath = picPath;
            resident.ApprovalStatus = "PENDING";
            resident.ApprovalNote = null;
            resident.UpdatedAt = now;

            if (user.Resident is null)
            {
                _dbContext.Residents.Add(resident);
                user.Resident = resident;
                user.ResidentId = resident.ResidentId;
            }

            // ====== 5) Reset & isi ulang anggota keluarga ======
            var existingMembers = _dbContext.ResidentFamilyMembers
                .Where(m => m.ResidentId == resident.ResidentId);
            _dbContext.ResidentFamilyMembers.RemoveRange(existingMembers);

            foreach (var memberRequest in request.FamilyMembers)
            {
                var member = new ResidentFamilyMember
                {
                    FamilyMemberId = Guid.NewGuid(),
                    RtId = rtId,
                    ResidentId = resident.ResidentId,
                    FullName = memberRequest.FullName,
                    BirthDate = memberRequest.BirthDate,
                    Gender = memberRequest.Gender,
                    Relationship = memberRequest.Relationship,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _dbContext.ResidentFamilyMembers.Add(member);
            }

            user.UpdatedAt = now;

            // ====== 6) Simpan DB lalu publish event, semuanya di dalam transaksi ======
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "ResidentProfileSubmitted", new
            {
                resident.ResidentId,
                resident.FullName,
                resident.NationalIdNumber
            }, user.UserId, cancellationToken);

            foreach (var member in request.FamilyMembers)
            {
                await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "FamilyMemberUpserted", new
                {
                    member.FullName,
                    member.Relationship
                }, user.UserId, cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);

            return Accepted(new { resident.ResidentId, resident.ApprovalStatus });
        }
        catch
        {
            // Rollback DB
            await tx.RollbackAsync(cancellationToken);

            // Hapus file yang baru ter-upload agar tidak orphan
            foreach (var rel in newFilesToCleanup)
            {
                try { DeleteFileIfExists(rel); } catch { /* swallow */ }
            }

            throw; // biar middleware/global handler yang menangani
        }
    }

    [HttpPost("daftarwarga")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> SubmitProfileWargaAsync([FromBody] ResidentProfileRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var user = await _dbContext.Users.Include(u => u.Resident).ThenInclude(r => r!.FamilyMembers)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.RtId == rtId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var resident = user.Resident ?? new Resident
        {
            ResidentId = Guid.NewGuid(),
            RtId = rtId,
            CreatedAt = now
        };

        string kkDocumentPath = string.Empty;
        string picPath = string.Empty;

        if (request.KkDocumentPath is not null && request.KkDocumentPath.Length > 0)
        {
            // tentukan folder upload, misal wwwroot/uploads/contributions/{rtId}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resident", "kk", rtId.ToString());
            Directory.CreateDirectory(root);

            var fileName = $"{Guid.NewGuid()}_{request.KkDocumentPath.FileName}";
            var fullPath = Path.Combine(root, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.KkDocumentPath.CopyToAsync(stream, cancellationToken);
            }

            // simpan path relatif yg bisa ditampilkan lagi ke FE
            kkDocumentPath = $"/uploads/resident/kk/{rtId}/{fileName}";
        }

        if (request.PicPath is not null && request.PicPath.Length > 0)
        {
            // tentukan folder upload, misal wwwroot/uploads/contributions/{rtId}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resident", "pic", rtId.ToString());
            Directory.CreateDirectory(root);

            var fileName = $"{Guid.NewGuid()}_{request.PicPath.FileName}";
            var fullPath = Path.Combine(root, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.PicPath.CopyToAsync(stream, cancellationToken);
            }

            // simpan path relatif yg bisa ditampilkan lagi ke FE
            picPath = $"/uploads/resident/pic/{rtId}/{fileName}";
        }

        resident.NationalIdNumber = request.NationalIdNumber;
        resident.FullName = request.FullName;
        resident.BirthDate = request.BirthDate;
        resident.Gender = request.Gender;
        resident.Blok = request.Blok;
        resident.PhoneNumber = request.PhoneNumber;
        resident.KkDocumentPath = kkDocumentPath;
        resident.PicPath = picPath;
        resident.ApprovalStatus = "PENDING";
        resident.ApprovalNote = null;
        resident.UpdatedAt = now;

        if (user.Resident is null)
        {
            _dbContext.Residents.Add(resident);
            user.Resident = resident;
            user.ResidentId = resident.ResidentId;
        }

        var existingMembers = _dbContext.ResidentFamilyMembers.Where(m => m.ResidentId == resident.ResidentId);
        _dbContext.ResidentFamilyMembers.RemoveRange(existingMembers);

        foreach (var memberRequest in request.FamilyMembers)
        {
            var member = new ResidentFamilyMember
            {
                FamilyMemberId = Guid.NewGuid(),
                RtId = rtId,
                ResidentId = resident.ResidentId,
                FullName = memberRequest.FullName,
                BirthDate = memberRequest.BirthDate,
                Gender = memberRequest.Gender,
                Relationship = memberRequest.Relationship,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.ResidentFamilyMembers.Add(member);
        }

        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "ResidentProfileSubmitted", new
        {
            resident.ResidentId,
            resident.FullName,
            resident.NationalIdNumber
        }, user.UserId, cancellationToken);

        foreach (var member in request.FamilyMembers)
        {
            await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId, "FamilyMemberUpserted", new
            {
                member.FullName,
                member.Relationship
            }, user.UserId, cancellationToken);
        }

        return Accepted(new { resident.ResidentId, resident.ApprovalStatus });
    }

    [HttpPost("{residentId:guid}/approval")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> ReviewResidentAsync(Guid residentId, [FromBody] ApproveResidentRequest request, CancellationToken cancellationToken)
    {
        var rtId = _tenantProvider.GetRtId();
        var userId = _tenantProvider.GetUserId();

        var resident = await _dbContext.Residents.FirstOrDefaultAsync(r => r.ResidentId == residentId && r.RtId == rtId, cancellationToken);
        if (resident is null)
        {
            return NotFound();
        }

        resident.ApprovalStatus = request.Approve ? "APPROVED" : "REJECTED";
        resident.ApprovalNote = request.Approve ? null : request.Note;
        resident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _eventPublisher.AppendAsync("RESIDENT", resident.ResidentId,
            request.Approve ? "ResidentProfileApproved" : "ResidentProfileRejected",
            new { resident.ResidentId, request.Note }, userId, cancellationToken);

        return Ok(new { resident.ResidentId, resident.ApprovalStatus, resident.ApprovalNote });
    }
}

