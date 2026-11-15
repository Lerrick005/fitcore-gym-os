using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Application.Setup.Validators;

namespace FitCore.Desktop.ViewModels.Wizard;

public sealed partial class BusinessIdentityViewModel : WizardStepViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _phonePrimary = string.Empty;
    [ObservableProperty] private string _email = string.Empty;

    public override string Title => "Your gym";
    public override string Description => "The essentials to get started. Everything else lives in Settings.";

    public BusinessIdentityViewModel(ISettingsService settings) { _settings = settings; }

    protected override async Task LoadAsyncCore(CancellationToken ct)
    {
        var b = await _settings.GetAsync<BrandingSettings>(ct);
        BusinessName = b.BusinessName;

        var c = await _settings.GetAsync<BusinessSettings>(ct);
        PhonePrimary = c.PhonePrimary;
        Email = c.Email;
    }

    protected override async Task<string?> ValidateAsyncCore(CancellationToken ct)
    {
        var branding = new BrandingSettings { BusinessName = BusinessName };
        var contacts = new BusinessSettings { PhonePrimary = PhonePrimary, Email = Email };

        var r1 = await new BusinessIdentityValidator().ValidateAsync(branding, ct);
        var r2 = await new BusinessContactValidator().ValidateAsync(contacts, ct);
        var errors = new System.Collections.Generic.List<string>();
        foreach (var e in r1.Errors) errors.Add(e.ErrorMessage);
        foreach (var e in r2.Errors) errors.Add(e.ErrorMessage);
        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    protected override async Task<Result> SaveAsyncCore(CancellationToken ct)
    {
        // Preserve existing values for fields not shown in the wizard
        var existingBranding = await _settings.GetAsync<BrandingSettings>(ct);
        var branding = existingBranding with { BusinessName = BusinessName };
        var r1 = await _settings.SaveAsync(branding, ct);
        if (!r1.IsSuccess) return r1;

        var existingContacts = await _settings.GetAsync<BusinessSettings>(ct);
        var contacts = existingContacts with { PhonePrimary = PhonePrimary, Email = Email };
        return await _settings.SaveAsync(contacts, ct);
    }
}

