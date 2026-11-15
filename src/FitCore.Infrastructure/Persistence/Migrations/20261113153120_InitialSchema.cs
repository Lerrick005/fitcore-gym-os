using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: true),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RulesJson = table.Column<string>(type: "TEXT", nullable: true),
                    UsageLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolidayOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OpenTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    CloseTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Installation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    AppVersionAtCreation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SetupCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    SetupStep = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenceRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RawLicenceJson = table.Column<string>(type: "TEXT", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenceRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    EmergencyContact = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsentRecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConsentVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MembershipPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PriceMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DurationValue = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationUnit = table.Column<int>(type: "INTEGER", nullable: false),
                    FreezeAllowanceDays = table.Column<int>(type: "INTEGER", nullable: true),
                    VisitLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    JoiningFeeMinor = table.Column<long>(type: "INTEGER", nullable: true),
                    JoiningFeeCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnabledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisabledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programmes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOwnerRole = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SequenceCounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NextValue = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequenceCounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettingsEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    JsonValue = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettingsEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Specialities = table.Column<string>(type: "TEXT", nullable: true),
                    AvailabilityJson = table.Column<string>(type: "TEXT", nullable: true),
                    CommissionRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckInUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CheckOutUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FollowUps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FollowUps_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyLedger_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WeightKg = table.Column<decimal>(type: "TEXT", nullable: true),
                    BodyFatPercent = table.Column<decimal>(type: "TEXT", nullable: true),
                    MeasurementsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Goal = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressEntries_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReferrerMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferralCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RefereeMemberId = table.Column<int>(type: "INTEGER", nullable: true),
                    ReferredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RewardGranted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Members_RefereeMemberId",
                        column: x => x.RefereeMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Members_ReferrerMemberId",
                        column: x => x.ReferrerMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PriceMinorSnapshot = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCodeSnapshot = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DurationSnapshot = table.Column<int>(type: "INTEGER", nullable: false),
                    TermsSnapshotJson = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountId = table.Column<int>(type: "INTEGER", nullable: true),
                    FrozenFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FrozenTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memberships_Discounts_DiscountId",
                        column: x => x.DiscountId,
                        principalTable: "Discounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Memberships_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Memberships_MembershipPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "MembershipPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProgrammeId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammeAssignments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgrammeAssignments_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    HashParams = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    AmountMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AttachmentPath = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    StockLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPriceMinor = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrainerAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerAssignments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainerAssignments_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    MembershipId = table.Column<int>(type: "INTEGER", nullable: true),
                    AmountMinor = table.Column<long>(type: "INTEGER", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedPaymentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "Memberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Payments_RelatedPaymentId",
                        column: x => x.RelatedPaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IssuedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    VoidedByReceiptId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receipts_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipts_Receipts_VoidedByReceiptId",
                        column: x => x.VoidedByReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_MemberId_CheckInUtc",
                table: "AttendanceRecords",
                columns: new[] { "MemberId", "CheckInUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TimestampUtc",
                table: "AuditLog",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BackupHistory_TimestampUtc",
                table: "BackupHistory",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_Code",
                table: "Discounts",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId",
                table: "Expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OccurredAtUtc",
                table: "Expenses",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SupplierId",
                table: "Expenses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_MemberId_Status",
                table: "FollowUps",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayOverrides_Date",
                table: "HolidayOverrides",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_SupplierId",
                table: "InventoryItems",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyLedger_MemberId_OccurredAtUtc",
                table: "LoyaltyLedger",
                columns: new[] { "MemberId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_FullName",
                table: "Members",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberNumber",
                table: "Members",
                column: "MemberNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Phone",
                table: "Members",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_DiscountId",
                table: "Memberships",
                column: "DiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_EndDate_Status",
                table: "Memberships",
                columns: new[] { "EndDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MemberId",
                table: "Memberships",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_PlanId",
                table: "Memberships",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Key",
                table: "Modules",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberId",
                table: "Payments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MembershipId",
                table: "Payments",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Method",
                table: "Payments",
                column: "Method");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedAtUtc",
                table: "Payments",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RelatedPaymentId",
                table: "Payments",
                column: "RelatedPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeAssignments_MemberId",
                table: "ProgrammeAssignments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeAssignments_ProgrammeId",
                table: "ProgrammeAssignments",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressEntries_MemberId_RecordedAtUtc",
                table: "ProgressEntries",
                columns: new[] { "MemberId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_PaymentId",
                table: "Receipts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReceiptNumber",
                table: "Receipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_VoidedByReceiptId",
                table: "Receipts",
                column: "VoidedByReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_RefereeMemberId",
                table: "Referrals",
                column: "RefereeMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralCode",
                table: "Referrals",
                column: "ReferralCode");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerMemberId",
                table: "Referrals",
                column: "ReferrerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SequenceCounters_Name",
                table: "SequenceCounters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettingsEntries_GroupKey",
                table: "SettingsEntries",
                column: "GroupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_InventoryItemId",
                table: "StockMovements",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAssignments_MemberId",
                table: "TrainerAssignments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAssignments_TrainerId",
                table: "TrainerAssignments",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        
            // PRS §13.5: append-only enforcement for Payments and Receipts.
            // No UPDATE or DELETE of financial records; corrections go through refund/void rows.
            migrationBuilder.Sql(@"
CREATE TRIGGER IF NOT EXISTS trg_Payments_NoUpdate
BEFORE UPDATE ON Payments
BEGIN
    SELECT RAISE(ABORT, 'Payments are append-only; use refund/void rows instead of UPDATE.');
END;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER IF NOT EXISTS trg_Payments_NoDelete
BEFORE DELETE ON Payments
BEGIN
    SELECT RAISE(ABORT, 'Payments are append-only; DELETE is not allowed.');
END;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER IF NOT EXISTS trg_Receipts_NoUpdate
BEFORE UPDATE ON Receipts
BEGIN
    SELECT RAISE(ABORT, 'Receipts are append-only; corrections must be new void rows.');
END;
");
            migrationBuilder.Sql(@"
CREATE TRIGGER IF NOT EXISTS trg_Receipts_NoDelete
BEFORE DELETE ON Receipts
BEGIN
    SELECT RAISE(ABORT, 'Receipts are append-only; DELETE is not allowed.');
END;
");}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BackupHistory");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FollowUps");

            migrationBuilder.DropTable(
                name: "HolidayOverrides");

            migrationBuilder.DropTable(
                name: "Installation");

            migrationBuilder.DropTable(
                name: "LicenceRecord");

            migrationBuilder.DropTable(
                name: "LoyaltyLedger");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "ProgrammeAssignments");

            migrationBuilder.DropTable(
                name: "ProgressEntries");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SequenceCounters");

            migrationBuilder.DropTable(
                name: "SettingsEntries");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "TrainerAssignments");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "Programmes");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "Trainers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "MembershipPlans");
        }
    }
}

