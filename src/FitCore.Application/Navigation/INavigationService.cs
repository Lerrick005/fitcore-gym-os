using FitCore.Application.Navigation;

namespace FitCore.Application.Navigation;

/// <summary>
/// Shell navigation (PRS §18). Hides entries the current user cannot access or whose module is disabled.
/// </summary>
public interface INavigationService
{
    /// <summary>Currently visible navigation items, in display order.</summary>
    IReadOnlyList<NavigationItem> VisibleItems { get; }

    /// <summary>Current page key (e.g. "dashboard", "members").</summary>
    string CurrentPageKey { get; }

    /// <summary>Raised when the visible items list is rebuilt or the current page changes.</summary>
    event EventHandler? NavigationChanged;

    /// <summary>Navigate to a page by key. No-op if not visible.</summary>
    Task NavigateAsync(string pageKey, CancellationToken ct = default);

    /// <summary>Navigate to a specific member detail page.</summary>
    Task NavigateToMemberAsync(int memberId, CancellationToken ct = default);

    /// <summary>Rebuild the visible items list based on current permission + module state.</summary>
    Task RefreshVisibleItemsAsync(CancellationToken ct = default);

    /// <summary>Navigate to the attendees page for a specific class (programme).</summary>
    Task NavigateToProgrammeAttendeesAsync(int programmeId, string className, string schedule, int capacity, CancellationToken ct = default);
}

