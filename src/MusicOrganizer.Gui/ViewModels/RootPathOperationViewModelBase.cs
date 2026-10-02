using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Adds the root-folder input shared by every <see cref="StreamingOperationViewModelBase{TOutcome}"/>
/// screen except <c>clean-journal</c>, which takes an age cutoff instead.
/// </summary>
/// <typeparam name="TOutcome">Per-item outcome type streamed by the wrapped use case.</typeparam>
public abstract partial class RootPathOperationViewModelBase<TOutcome> : StreamingOperationViewModelBase<TOutcome>
{
    [ObservableProperty]
    public partial string? RootPath { get; set; }
}
