namespace FitCore.Application.Authorization;

/// <summary>
/// Default roles (PRS §11). Names are renamable; the assignments are editable.
/// Owner is the only role with IsOwnerRole = true.
/// </summary>
public static class DefaultRoleCatalogue
{
    public sealed record RoleDef(string Name, bool IsOwner, IReadOnlyList<string> Permissions);

    public static readonly IReadOnlyList<RoleDef> All = new[]
    {
        // Owner: everything, marked as the owner role
        new RoleDef("Owner", true,
            PermissionCatalogue.All.Select(p => p.Key).ToArray()),

        // Manager: everything except user/role management and branding
        new RoleDef("Manager", false, PermissionCatalogue.All
            .Where(p => p.Key is not ("users.manage" or "roles.manage" or "branding.manage" or "modules.manage" or "settings.manage"))
            .Select(p => p.Key).ToArray()),

        // Receptionist: front-desk operations
        new RoleDef("Receptionist", false, new[]
        {
            "members.view", "members.create", "members.edit",
            "memberships.sell", "memberships.freeze",
            "payments.record", "receipts.reprint",
            "attendance.checkin", "reports.view"
        }),

        // Trainer: member oversight + progress + programmes
        new RoleDef("Trainer", false, new[]
        {
            "members.view",
            "progress.record",
            "programmes.manage",
            "trainers.manage",
            "attendance.checkin"
        }),

        // Accountant: financial visibility + expense recording
        new RoleDef("Accountant", false, new[]
        {
            "members.view",
            "payments.record", "payments.refund",
            "receipts.reprint",
            "reports.view", "reports.export",
            "expenses.record"
        }),
    };
}
