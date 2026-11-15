using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Common;
using FitCore.Application.Members;

namespace FitCore.Desktop.ViewModels;

/// <summary>Backs the add/edit member modal. LoadAsync with memberId = null means "create".</summary>
public sealed partial class MemberEditViewModel : ObservableObject
{
    private readonly IMemberService _members;
    private int? _memberId;

    [ObservableProperty] private string _title = "Add member";
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private DateTime? _dateOfBirth;
    [ObservableProperty] private string _gender = string.Empty;
    [ObservableProperty] private string _emergencyContact = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public MemberEditViewModel(IMemberService members) => _members = members;

    public async Task InitializeAsync(int? memberId, CancellationToken ct = default)
    {
        _memberId = memberId;
        if (memberId is null)
        {
            Title = "Add member";
            return;
        }

        Title = "Edit member";
        var result = await _members.GetAsync(memberId.Value, ct);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }
        var m = result.Value!;
        FullName = m.FullName;
        Phone = m.Phone;
        Email = m.Email ?? string.Empty;
        DateOfBirth = m.DateOfBirth;
        Gender = m.Gender ?? string.Empty;
        EmergencyContact = m.EmergencyContact ?? string.Empty;
        Notes = m.Notes ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var input = new MemberInput(
                FullName, Phone, Email, DateOfBirth,
                string.IsNullOrWhiteSpace(Gender) ? null : Gender,
                string.IsNullOrWhiteSpace(EmergencyContact) ? null : EmergencyContact,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes);

            Result<FitCore.Domain.Entities.Member> result;
            if (_memberId is null)
                result = await _members.CreateAsync(input);
            else
                result = await _members.UpdateAsync(_memberId.Value, input);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.ErrorMessage ?? "Save failed.";
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
