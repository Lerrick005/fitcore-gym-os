using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Abstractions;
using FitCore.Application.Payments;
using FitCore.Application.Settings;

namespace FitCore.Desktop.ViewModels.Members;

public sealed partial class RecordPaymentViewModel : ObservableObject
{
    private readonly IPaymentService _payments;
    private readonly ISettingsService _settings;
    private int _memberId;
    private int? _membershipId;

    [ObservableProperty] private string _amountDisplay = string.Empty;
    [ObservableProperty] private string _method = "Cash";
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public string[] Methods { get; } = new[] { "Cash", "M-Pesa", "Card", "Bank transfer", "Other" };

    public event EventHandler<bool>? CloseRequested;

    public RecordPaymentViewModel(IPaymentService payments, ISettingsService settings)
    {
        _payments = payments;
        _settings = settings;
    }

    public async Task InitializeAsync(int memberId, int? membershipId, CancellationToken ct = default)
    {
        _memberId = memberId;
        _membershipId = membershipId;

        var locale = await _settings.GetAsync<LocaleSettings>(ct);
        Method = "Cash";
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        ErrorMessage = null;
        if (!decimal.TryParse(AmountDisplay, out var amountDecimal) || amountDecimal <= 0)
        {
            ErrorMessage = "Enter a valid amount.";
            return;
        }

        IsBusy = true;
        try
        {
            var locale = await _settings.GetAsync<LocaleSettings>();
            var amountMinor = (long)(amountDecimal * (decimal)Math.Pow(10, locale.DecimalPlaces));

            var request = new RecordPaymentRequest(
                _memberId,
                _membershipId,
                amountMinor,
                locale.CurrencyCode,
                Method,
                string.IsNullOrWhiteSpace(Reference) ? null : Reference,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            var result = await _payments.RecordAsync(request);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            CloseRequested?.Invoke(this, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
