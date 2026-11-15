using FitCore.Domain.Entities;
using FitCore.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FitCore.Infrastructure.Persistence;

public class FitCoreDbContext : DbContext
{
    public FitCoreDbContext(DbContextOptions<FitCoreDbContext> options) : base(options) { }

    // §13.1
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<SettingsEntry> SettingsEntries => Set<SettingsEntry>();
    public DbSet<ModuleState> Modules => Set<ModuleState>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LicenceRecord> LicenceRecords => Set<LicenceRecord>();
    public DbSet<BackupHistory> BackupHistories => Set<BackupHistory>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();

    // §13.2
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RecoveryAttempt> RecoveryAttempts => Set<RecoveryAttempt>();

    // §13.3
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<HolidayOverride> HolidayOverrides => Set<HolidayOverride>();

    // §13.4
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<TrainerAssignment> TrainerAssignments => Set<TrainerAssignment>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<ProgrammeAssignment> ProgrammeAssignments => Set<ProgrammeAssignment>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<ProgressEntry> ProgressEntries => Set<ProgressEntry>();
    public DbSet<WorkoutEntry> WorkoutEntries => Set<WorkoutEntry>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<LoyaltyLedgerEntry> LoyaltyLedger => Set<LoyaltyLedgerEntry>();
    public DbSet<LoyaltyReward> LoyaltyRewards => Set<LoyaltyReward>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new InstallationConfig());
        modelBuilder.ApplyConfiguration(new SettingsEntryConfig());
        modelBuilder.ApplyConfiguration(new ModuleStateConfig());
        modelBuilder.ApplyConfiguration(new PermissionConfig());
        modelBuilder.ApplyConfiguration(new RoleConfig());
        modelBuilder.ApplyConfiguration(new RolePermissionConfig());
        modelBuilder.ApplyConfiguration(new AuditLogConfig());
        modelBuilder.ApplyConfiguration(new LicenceRecordConfig());
        modelBuilder.ApplyConfiguration(new BackupHistoryConfig());
        modelBuilder.ApplyConfiguration(new SequenceCounterConfig());

        modelBuilder.ApplyConfiguration(new UserConfig());
        modelBuilder.ApplyConfiguration(new UserSessionConfig());
        modelBuilder.ApplyConfiguration(new RecoveryAttemptConfig());

        modelBuilder.ApplyConfiguration(new MemberConfig());
        modelBuilder.ApplyConfiguration(new MembershipPlanConfig());
        modelBuilder.ApplyConfiguration(new MembershipConfig());
        modelBuilder.ApplyConfiguration(new DiscountConfig());
        modelBuilder.ApplyConfiguration(new PaymentConfig());
        modelBuilder.ApplyConfiguration(new ReceiptConfig());
        modelBuilder.ApplyConfiguration(new AttendanceRecordConfig());
        modelBuilder.ApplyConfiguration(new HolidayOverrideConfig());

        modelBuilder.ApplyConfiguration(new TrainerConfig());
        modelBuilder.ApplyConfiguration(new TrainerAssignmentConfig());
        modelBuilder.ApplyConfiguration(new ProgrammeConfig());
        modelBuilder.ApplyConfiguration(new ProgrammeAssignmentConfig());
        modelBuilder.ApplyConfiguration(new ProgressEntryConfig());
        modelBuilder.ApplyConfiguration(new InventoryItemConfig());
        modelBuilder.ApplyConfiguration(new StockMovementConfig());
        modelBuilder.ApplyConfiguration(new SupplierConfig());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfig());
        modelBuilder.ApplyConfiguration(new ExpenseConfig());
        modelBuilder.ApplyConfiguration(new FollowUpConfig());
        modelBuilder.ApplyConfiguration(new ReferralConfig());
        modelBuilder.ApplyConfiguration(new LoyaltyLedgerEntryConfig());
        modelBuilder.ApplyConfiguration(new LoyaltyRewardConfig());
    }
}



