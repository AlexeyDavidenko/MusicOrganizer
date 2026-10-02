using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Shell view model: owns one instance of every screen and switches which one is displayed based
/// on the sidebar selection.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new <see cref="MainWindowViewModel"/>.
    /// </summary>
    /// <param name="scan">Scan screen.</param>
    /// <param name="recoverTags">Recover-tags screen.</param>
    /// <param name="fixEncoding">Fix-encoding screen.</param>
    /// <param name="rename">Rename screen.</param>
    /// <param name="organize">Organize screen.</param>
    /// <param name="findDuplicates">Find-duplicates screen.</param>
    /// <param name="removeDuplicates">Remove-duplicates screen.</param>
    /// <param name="cleanJournal">Clean-journal screen.</param>
    /// <param name="rollback">Rollback screen.</param>
    public MainWindowViewModel(
        ScanViewModel scan,
        RecoverTagsViewModel recoverTags,
        FixEncodingViewModel fixEncoding,
        RenameViewModel rename,
        OrganizeViewModel organize,
        FindDuplicatesViewModel findDuplicates,
        RemoveDuplicatesViewModel removeDuplicates,
        CleanJournalViewModel cleanJournal,
        RollbackViewModel rollback)
    {
        NavigationItems =
        [
            new NavigationItem("Scan", scan),
            new NavigationItem("Recover Tags", recoverTags),
            new NavigationItem("Fix Encoding", fixEncoding),
            new NavigationItem("Rename", rename),
            new NavigationItem("Organize", organize),
            new NavigationItem("Find Duplicates", findDuplicates),
            new NavigationItem("Remove Duplicates", removeDuplicates),
            new NavigationItem("Clean Journal", cleanJournal),
            new NavigationItem("Rollback", rollback),
        ];

        SelectedNavigationItem = NavigationItems[0];
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    public partial NavigationItem? SelectedNavigationItem { get; set; }

    /// <summary>Screen currently shown in the main content area.</summary>
    public ViewModelBase? CurrentPage => SelectedNavigationItem?.ViewModel;

    /// <summary>Every screen, in sidebar display order — one entry per CLI command.</summary>
    public IReadOnlyList<NavigationItem> NavigationItems { get; }
}
