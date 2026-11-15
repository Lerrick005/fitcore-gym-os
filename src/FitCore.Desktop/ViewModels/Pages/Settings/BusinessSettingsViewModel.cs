using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Application.Setup.Validators;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class BusinessSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _phonePrimary = string.Empty;
    [ObservableProperty] private string _phoneSecondary = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _website = string.Empty;
    [ObservableProperty] private string _physicalAddress = string.Empty;
    [ObservableProperty] private string _registrationNumber = string.Empty;
    [ObservableProperty] private string _taxNumber = string.Empty;
    [ObservableProperty] private string _socialHandles = string.Empty;

    public override string GroupName => "Business & contacts";
    public override string GroupDescription => "Contact details and legal information shown to customers.";

    public BusinessSettingsViewModel(ISettingsService settings) => _settings = settings;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync<BusinessSettings>(ct);
        PhonePrimary = s.PhonePrimary;
        PhoneSecondary = s.PhoneSecondary;
        Email = s.Email;
        Website = s.Website;
        PhysicalAddress = s.PhysicalAddress;
        RegistrationNumber = s.RegistrationNumber;
        TaxNumber = s.TaxNumber;
        SocialHandles = s.SocialHandles;
    }

    protected override async Task<Result> SaveCoreAsync(CancellationToken ct)
    {
        var s = new BusinessSettings
        {
            PhonePrimary = PhonePrimary,
            PhoneSecondary = PhoneSecondary,
            Email = Email,
            Website = Website,
            PhysicalAddress = PhysicalAddress,
            RegistrationNumber = RegistrationNumber,
            TaxNumber = TaxNumber,
            SocialHandles = SocialHandles
        };
        var v = await new BusinessContactValidator().ValidateAsync(s, ct);
        if (!v.IsValid) return Result.Fail("settings.validation", string.Join(" ", v.Errors.ConvertAll(e => e.ErrorMessage)));
        return await _settings.SaveAsync(s, ct);
    }
}
