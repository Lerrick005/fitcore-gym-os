using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Settings;
using FitCore.Application.Setup.Validators;

namespace FitCore.Desktop.ViewModels.Pages.Settings;

public sealed partial class DayHoursViewModel : ObservableObject
{
    [ObservableProperty] private string _dayName = string.Empty;
    [ObservableProperty] private bool _isClosed;
    [ObservableProperty] private string _openTime = "06:00";
    [ObservableProperty] private string _closeTime = "22:00";
    [ObservableProperty] private string _splitOpenTime = string.Empty;
    [ObservableProperty] private string _splitCloseTime = string.Empty;

    public DayHours ToModel() => new()
    {
        IsClosed = IsClosed,
        OpenTime = OpenTime,
        CloseTime = CloseTime,
        SplitOpenTime = string.IsNullOrWhiteSpace(SplitOpenTime) ? null : SplitOpenTime,
        SplitCloseTime = string.IsNullOrWhiteSpace(SplitCloseTime) ? null : SplitCloseTime
    };

    public void Load(DayHours m)
    {
        IsClosed = m.IsClosed;
        OpenTime = m.OpenTime;
        CloseTime = m.CloseTime;
        SplitOpenTime = m.SplitOpenTime ?? string.Empty;
        SplitCloseTime = m.SplitCloseTime ?? string.Empty;
    }
}

public sealed partial class HoursSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ISettingsService _settings;

    public DayHoursViewModel Monday { get; } = new() { DayName = "Monday" };
    public DayHoursViewModel Tuesday { get; } = new() { DayName = "Tuesday" };
    public DayHoursViewModel Wednesday { get; } = new() { DayName = "Wednesday" };
    public DayHoursViewModel Thursday { get; } = new() { DayName = "Thursday" };
    public DayHoursViewModel Friday { get; } = new() { DayName = "Friday" };
    public DayHoursViewModel Saturday { get; } = new() { DayName = "Saturday" };
    public DayHoursViewModel Sunday { get; } = new() { DayName = "Sunday" };

    [ObservableProperty] private bool _warnOutsideHours = true;
    [ObservableProperty] private bool _blockOutsideHours;

    public override string GroupName => "Operating hours";
    public override string GroupDescription => "When the gym is open. Check-ins respect these hours per policy.";

    public HoursSettingsViewModel(ISettingsService settings) => _settings = settings;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync<HoursSettings>(ct);
        Monday.Load(s.Monday); Tuesday.Load(s.Tuesday); Wednesday.Load(s.Wednesday);
        Thursday.Load(s.Thursday); Friday.Load(s.Friday); Saturday.Load(s.Saturday);
        Sunday.Load(s.Sunday);
        WarnOutsideHours = s.WarnOutsideHours;
        BlockOutsideHours = s.BlockOutsideHours;
    }

    protected override async Task<Result> SaveCoreAsync(CancellationToken ct)
    {
        var s = new HoursSettings
        {
            Monday = Monday.ToModel(),
            Tuesday = Tuesday.ToModel(),
            Wednesday = Wednesday.ToModel(),
            Thursday = Thursday.ToModel(),
            Friday = Friday.ToModel(),
            Saturday = Saturday.ToModel(),
            Sunday = Sunday.ToModel(),
            WarnOutsideHours = WarnOutsideHours,
            BlockOutsideHours = BlockOutsideHours
        };
        var v = await new HoursValidator().ValidateAsync(s, ct);
        if (!v.IsValid) return Result.Fail("settings.validation", string.Join(" ", v.Errors.ConvertAll(e => e.ErrorMessage)));

        return await _settings.SaveAsync(s, ct);
    }
}
