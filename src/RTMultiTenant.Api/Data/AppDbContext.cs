using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Entities;

namespace RTMultiTenant.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Rt> Rts => Set<Rt>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventRecord> EventStore => Set<EventRecord>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<ResidentFamilyMember> ResidentFamilyMembers => Set<ResidentFamilyMember>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<CashExpense> CashExpenses => Set<CashExpense>();
    public DbSet<MonthlyCashSummary> MonthlyCashSummaries => Set<MonthlyCashSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rt>(entity =>
        {
            entity.ToTable("rt");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.RtNumber).HasColumnName("rt_number").HasMaxLength(10);
            entity.Property(e => e.RwNumber).HasColumnName("rw_number").HasMaxLength(10);
            entity.Property(e => e.VillageName).HasColumnName("village_name").HasMaxLength(100);
            entity.Property(e => e.SubdistrictName).HasColumnName("subdistrict_name").HasMaxLength(100);
            entity.Property(e => e.CityName).HasColumnName("city_name").HasMaxLength(100);
            entity.Property(e => e.ProvinceName).HasColumnName("province_name").HasMaxLength(100);
            entity.Property(e => e.AddressDetail).HasColumnName("address_detail").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => new { e.RtNumber, e.RwNumber, e.VillageName, e.SubdistrictName, e.CityName, e.ProvinceName })
                .IsUnique()
                .HasDatabaseName("uq_rt_context");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(10);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("event_store");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id");
            entity.Property(e => e.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100);
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100);
            entity.Property(e => e.EventPayload).HasColumnName("event_payload");
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at");
            entity.Property(e => e.CausedByUserId).HasColumnName("caused_by_user_id");
            entity.Property(e => e.AggregateVersion).HasColumnName("aggregate_version");
            entity.HasIndex(e => new { e.RtId, e.AggregateId, e.AggregateVersion }).HasDatabaseName("idx_rt_agg_ver");
            entity.HasIndex(e => new { e.RtId, e.AggregateType, e.OccurredAt }).HasDatabaseName("idx_rt_type_time");
            entity.HasIndex(e => new { e.RtId, e.CausedByUserId, e.OccurredAt }).HasDatabaseName("idx_rt_user_time");
        });

        modelBuilder.Entity<Resident>(entity =>
        {
            entity.ToTable("residents");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.NationalIdNumber).HasColumnName("national_id_number").HasMaxLength(32);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(1);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
            entity.Property(e => e.KkDocumentPath).HasColumnName("kk_document_path").HasMaxLength(255);
            entity.Property(e => e.ApprovalStatus).HasColumnName("approval_status").HasMaxLength(20);
            entity.Property(e => e.ApprovalNote).HasColumnName("approval_note").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.RtId).HasDatabaseName("idx_rt_resident");
            entity.HasIndex(e => new { e.RtId, e.ApprovalStatus }).HasDatabaseName("idx_rt_status");
        });

        modelBuilder.Entity<ResidentFamilyMember>(entity =>
        {
            entity.ToTable("resident_family_members");
            entity.Property(e => e.FamilyMemberId).HasColumnName("family_member_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100);
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(1);
            entity.Property(e => e.Relationship).HasColumnName("relationship").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => new { e.RtId, e.ResidentId }).HasDatabaseName("idx_rt_family");
        });

        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.ToTable("contributions");
            entity.Property(e => e.ContributionId).HasColumnName("contribution_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.ResidentId).HasColumnName("resident_id");
            entity.Property(e => e.PeriodStart).HasColumnName("period_start");
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
            entity.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(14,2)");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.ProofImagePath).HasColumnName("proof_image_path").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
            entity.Property(e => e.AdminNote).HasColumnName("admin_note").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => e.RtId).HasDatabaseName("idx_rt_contrib");
            entity.HasIndex(e => new { e.RtId, e.ResidentId }).HasDatabaseName("idx_rt_resident");
            entity.HasIndex(e => new { e.RtId, e.Status }).HasDatabaseName("idx_rt_status");
            entity.HasIndex(e => new { e.RtId, e.PeriodStart, e.PeriodEnd }).HasDatabaseName("idx_rt_period");
        });

        modelBuilder.Entity<CashExpense>(entity =>
        {
            entity.ToTable("cash_expenses");
            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.ExpenseDate).HasColumnName("expense_date");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(14,2)");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.HasIndex(e => new { e.RtId, e.ExpenseDate }).HasDatabaseName("idx_rt_expense_date");
            entity.HasIndex(e => new { e.RtId, e.IsActive }).HasDatabaseName("idx_rt_active");
        });

        modelBuilder.Entity<MonthlyCashSummary>(entity =>
        {
            entity.ToTable("monthly_cash_summary");
            entity.Property(e => e.SummaryId).HasColumnName("summary_id");
            entity.Property(e => e.RtId).HasColumnName("rt_id");
            entity.Property(e => e.Year).HasColumnName("year");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.TotalContributionIn).HasColumnName("total_contribution_in").HasColumnType("decimal(14,2)");
            entity.Property(e => e.TotalExpenseOut).HasColumnName("total_expense_out").HasColumnType("decimal(14,2)");
            entity.Property(e => e.BalanceEnd).HasColumnName("balance_end").HasColumnType("decimal(14,2)");
            entity.Property(e => e.GeneratedAt).HasColumnName("generated_at");
            entity.HasIndex(e => new { e.RtId, e.Year, e.Month }).IsUnique().HasDatabaseName("uq_rt_year_month");
        });
    }
}
