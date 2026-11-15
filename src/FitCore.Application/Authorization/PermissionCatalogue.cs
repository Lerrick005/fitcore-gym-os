namespace FitCore.Application.Authorization;

/// <summary>
/// Fixed permission catalogue (PRS §11). Seeded as system constants; not user-editable.
/// Adding a permission = add to this list, bump the schema version, and reseed.
/// </summary>
public static class PermissionCatalogue
{
    public sealed record PermissionDef(string Key, string Category, string Description);

    public static readonly IReadOnlyList<PermissionDef> All = new[]
    {
        // Members
        new PermissionDef("members.view", "Members", "View member records and search"),
        new PermissionDef("members.create", "Members", "Register new members"),
        new PermissionDef("members.edit", "Members", "Edit member details"),
        new PermissionDef("members.archive", "Members", "Archive members"),
        new PermissionDef("members.import", "Members", "Import members from CSV/XLSX"),

        // Memberships
        new PermissionDef("memberships.sell", "Memberships", "Sell memberships"),
        new PermissionDef("memberships.freeze", "Memberships", "Freeze and unfreeze memberships"),
        new PermissionDef("memberships.override", "Memberships", "Override membership rules"),

        // Payments
        new PermissionDef("payments.record", "Payments", "Record payments"),
        new PermissionDef("payments.refund", "Payments", "Issue refunds"),
        new PermissionDef("payments.void", "Payments", "Void payments"),

        // Receipts
        new PermissionDef("receipts.reprint", "Receipts", "Reprint receipts (marked Duplicate)"),

        // Attendance
        new PermissionDef("attendance.checkin", "Attendance", "Check members in"),
        new PermissionDef("attendance.edit", "Attendance", "Correct attendance records"),

        // Reports
        new PermissionDef("reports.view", "Reports", "View reports and dashboard"),
        new PermissionDef("reports.export", "Reports", "Export reports to CSV/Excel/PDF"),

        // Settings
        new PermissionDef("settings.view", "Settings", "View settings"),
        new PermissionDef("settings.manage", "Settings", "Change settings"),
        new PermissionDef("branding.manage", "Settings", "Change branding and logo"),

        // Administration
        new PermissionDef("modules.manage", "Administration", "Enable/disable modules"),
        new PermissionDef("users.manage", "Administration", "Create and edit user accounts"),
        new PermissionDef("roles.manage", "Administration", "Manage roles and permissions"),
        new PermissionDef("backup.manage", "Administration", "Create and restore backups"),
        new PermissionDef("audit.view", "Administration", "View audit log"),

        // Module contributions (§9.1)
        new PermissionDef("trainers.manage", "Trainers", "Manage trainer profiles"),
        new PermissionDef("programmes.manage", "Programmes", "Manage workout programmes"),
        new PermissionDef("progress.record", "Progress", "Record member progress entries"),
        new PermissionDef("inventory.manage", "Inventory", "Manage inventory items"),
        new PermissionDef("expenses.record", "Expenses", "Record expenses"),
        new PermissionDef("followups.manage", "Retention", "Manage follow-ups"),
        new PermissionDef("loyalty.manage", "Loyalty", "Manage referrals and loyalty"),
    };

    public static int Count => All.Count;
}
