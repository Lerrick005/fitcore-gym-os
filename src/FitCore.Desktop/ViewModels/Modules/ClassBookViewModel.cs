using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitCore.Application.Classes;
using FitCore.Application.Members;

namespace FitCore.Desktop.ViewModels.Modules;

public sealed partial class ClassBookViewModel : ObservableObject
{
    private readonly IClassesService _classes;
    private readonly IMemberService _members;
    private int _programmeId;

    public ObservableCollection<MemberListItem> Members { get; } = new();

    [ObservableProperty] private string _className = string.Empty;
    [ObservableProperty] private MemberListItem? _selectedMember;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isBusy;

    public event EventHandler<bool>? CloseRequested;

    public ClassBookViewModel(IClassesService classes, IMemberService members)
    {
        _classes = classes;
        _members = members;
    }

    public async Task InitializeAsync(int programmeId, string className, CancellationToken ct = default)
    {
        _programmeId = programmeId;
        ClassName = className;

        var r = await _members.SearchAsync(new MemberSearchQuery { Take = 500 }, ct);
        Members.Clear();
        if (r.IsSuccess) foreach (var m in r.Value!) Members.Add(m);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedMember is null) { ErrorMessage = "Select a member."; return; }

        IsBusy = true;
        try
        {
            var r = await _classes.BookAsync(_programmeId, SelectedMember.Id);
            if (!r.IsSuccess) { ErrorMessage = r.ErrorMessage; return; }
            CloseRequested?.Invoke(this, true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
