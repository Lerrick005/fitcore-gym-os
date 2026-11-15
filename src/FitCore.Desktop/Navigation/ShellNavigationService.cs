using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Navigation;

namespace FitCore.Desktop.Navigation;

/// <summary>
/// Concrete navigation service. Lives in Desktop because it holds UI-facing state.
/// Filters NavigationCatalog by permission and module state.
/// </summary>
public sealed class ShellNavigationService : INavigationService
{
    private readonly ICurrentUser _user;
    private readonly IModuleAccessGuard _modules;

    private IReadOnlyList<NavigationItem> _visibleItems = Array.Empty<NavigationItem>();

    public event EventHandler? NavigationChanged;

    public string CurrentPageKey { get; private set; } = "dashboard";

    public IReadOnlyList<NavigationItem> VisibleItems => _visibleItems;

    public ShellNavigationService(ICurrentUser user, IModuleAccessGuard modules)
    {
        _user = user;
        _modules = modules;
    }

    public async Task RefreshVisibleItemsAsync(CancellationToken ct = default)
    {
        var visible = new List<NavigationItem>();

        foreach (var item in NavigationCatalog.All)
        {
            if (item.RequiredPermission is not null && !_user.HasPermission(item.RequiredPermission))
                continue;

            if (item.RequiredModule is not null)
            {
                if (!await _modules.IsEnabledAsync(item.RequiredModule, ct))
                    continue;
            }

            visible.Add(item);
        }

        _visibleItems = visible;

        // If current page is no longer visible, fall back to dashboard
        if (_visibleItems.All(i => i.Key != CurrentPageKey))
            CurrentPageKey = "dashboard";

        NavigationChanged?.Invoke(this, EventArgs.Empty);
    }

    public int? CurrentMemberId { get; private set; }
    public int? CurrentProgrammeId { get; private set; }

    public Task NavigateToMemberAsync(int memberId, CancellationToken ct = default)
    {
        CurrentMemberId = memberId;
        CurrentPageKey = "member-detail";
        NavigationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task NavigateToProgrammeAttendeesAsync(int programmeId, string className, string schedule, int capacity, CancellationToken ct = default)
    {
        CurrentProgrammeId = programmeId;
        // Store class info for the VM to pick up
        CurrentAttendeesClassName = className;
        CurrentAttendeesSchedule = schedule;
        CurrentAttendeesCapacity = capacity;
        await NavigateAsync("class-attendees", ct);
    }

    public string CurrentAttendeesClassName { get; private set; } = string.Empty;
    public string CurrentAttendeesSchedule { get; private set; } = string.Empty;
    public int CurrentAttendeesCapacity { get; private set; } = 20;

    // Called every navigation to pick up module changes
    public async Task NavigateAsync(string pageKey, CancellationToken ct = default)
    {
        if (_visibleItems.All(i => i.Key != pageKey))
            return; // protected page

        CurrentPageKey = pageKey;
        CurrentMemberId = null;
        await RefreshVisibleItemsAsync(ct);
        NavigationChanged?.Invoke(this, EventArgs.Empty);
    }
}



