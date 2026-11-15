using CommunityToolkit.Mvvm.ComponentModel;

namespace FitCore.Desktop.ViewModels.Pages;

/// <summary>Base for every page shown in the shell content area.</summary>
public abstract partial class PageViewModelBase : ObservableObject
{
    public abstract string Title { get; }

    /// <summary>Called when the page becomes visible. Override to load data.</summary>
    public virtual Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
}
