using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.ViewModels.Pages;

public sealed partial class SettingsSectionItem
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = string.Empty;
}

public sealed partial class SettingsViewModel : PageViewModelBase
{
    public ObservableCollection<SettingsSectionItem> Sections { get; } = new();

    public event EventHandler<string>? SectionSelected;

    public override string Title => "Settings";

    public SettingsViewModel()
    {
        Sections.Add(new SettingsSectionItem
        {
            Key = "branding", Title = "Branding",
            Description = "Business name, colours, logo",
            IconGlyph = "\uE790"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "business", Title = "Business & contacts",
            Description = "Phones, email, address, legal",
            IconGlyph = "\uE8F1"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "locale", Title = "Locale & formats",
            Description = "Currency, time zone, date formats",
            IconGlyph = "\uE909"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "hours", Title = "Operating hours",
            Description = "When the gym is open",
            IconGlyph = "\uE823"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "plans", Title = "Plans & services",
            Description = "What you sell to members",
            IconGlyph = "\uE8EC"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "users", Title = "Users & roles",
            Description = "Staff accounts and permissions",
            IconGlyph = "\uE779"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "backup", Title = "Backup & restore",
            Description = "Protect your data",
            IconGlyph = "\uE74E"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "licence", Title = "Licence",
            Description = "Vendor-managed licence details",
            IconGlyph = "\uE72E"
        });
        Sections.Add(new SettingsSectionItem
        {
            Key = "modules", Title = "Modules",
            Description = "Enable optional features",
            IconGlyph = "\uE71D"
        });
    }

    [RelayCommand]
    private void OpenSection(SettingsSectionItem? section)
    {
        if (section is null) return;
        SectionSelected?.Invoke(this, section.Key);
    }
}






