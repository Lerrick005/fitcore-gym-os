namespace FitCore.Application.Navigation;

/// <summary>
/// Full catalogue of navigable pages. Adding a page = adding an entry here.
/// Groups: "Operations", "Management", "System".
/// </summary>
public static class NavigationCatalog
{
    public static readonly IReadOnlyList<NavigationItem> All = new[]
    {
        // Main (no group header)
        new NavigationItem("dashboard", "Dashboard", "\uE80F", null, null, ""),

        // Operations
        new NavigationItem("members", "Members", "\uE77B", "members.view", null, "Operations"),
        new NavigationItem("attendance", "Attendance", "\uE8F1", "attendance.checkin", null, "Operations"),
        new NavigationItem("fitness", "Fitness", "\uE95E", "programmes.manage", "programmes", "Operations"),
        new NavigationItem("payments", "Payments", "\uE8C7", "payments.record", null, "Operations"),

        // Management
        new NavigationItem("plans", "Membership Plans", "\uE8EC", "memberships.sell", null, "Management"),
        new NavigationItem("trainers", "Trainers", "\uE716", "trainers.manage", "trainers", "Management"),
        new NavigationItem("inventory", "Inventory", "\uE7B8", "inventory.manage", "inventory", "Management"),
        new NavigationItem("expenses", "Expenses", "\uE8C4", "expenses.record", "expenses", "Management"),
        new NavigationItem("retention", "Retention", "\uE81C", "followups.manage", "retention", "Management"),
        new NavigationItem("loyalty", "Loyalty", "\uE735", "loyalty.manage", "loyalty", "Management"),
        new NavigationItem("classes", "Classes & Programs", "\uE7C1", "programmes.manage", "programmes", "Management"),

        // System
        new NavigationItem("reports", "Reports", "\uE9D9", "reports.view", null, "System"),
        new NavigationItem("cashup", "Cash-up", "\uE8C7", "payments.record", null, "Operations"),
        new NavigationItem("settings", "Settings", "\uE713", "settings.view", null, "System")
    };
}



