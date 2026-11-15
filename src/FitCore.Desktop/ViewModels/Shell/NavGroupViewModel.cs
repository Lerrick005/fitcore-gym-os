using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Navigation;

namespace FitCore.Desktop.ViewModels.Shell;

/// <summary>One sidebar group with items. Empty GroupName means no header.</summary>
public sealed partial class NavGroupViewModel : ObservableObject
{
    public string GroupName { get; }
    public string DisplayName { get; }
    public ObservableCollection<NavItemViewModel> Items { get; } = new();

    public NavGroupViewModel(string groupName)
    {
        GroupName = groupName;
        DisplayName = groupName.ToUpperInvariant();
    }
}

/// <summary>One clickable sidebar item.</summary>
public sealed partial class NavItemViewModel : ObservableObject
{
    private bool _isActive;

    public string Key { get; }
    public string DisplayName { get; }
    public string IconGlyph { get; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private bool _isDisabled;
    public bool IsDisabled
    {
        get => _isDisabled;
        set => SetProperty(ref _isDisabled, value);
    }

    public NavItemViewModel(NavigationItem item)
    {
        Key = item.Key;
        DisplayName = item.DisplayName;
        IconGlyph = item.IconGlyph;
    }
}

