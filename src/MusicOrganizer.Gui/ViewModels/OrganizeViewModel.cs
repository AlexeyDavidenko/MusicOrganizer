using CommunityToolkit.Mvvm.ComponentModel;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="OrganizeEngine"/> — the <c>organize</c> command.
/// </summary>
public sealed partial class OrganizeViewModel : RootPathOperationViewModelBase<RenameOutcome>
{
    private readonly OrganizeEngine _organizeEngine;

    /// <summary>
    /// Creates a new <see cref="OrganizeViewModel"/>.
    /// </summary>
    /// <param name="organizeEngine">Use case this screen wraps.</param>
    public OrganizeViewModel(OrganizeEngine organizeEngine)
    {
        _organizeEngine = organizeEngine;
    }

    [ObservableProperty]
    public partial bool PruneEmptyFolders { get; set; }

    /// <inheritdoc/>
    protected override IAsyncEnumerable<RenameOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _organizeEngine.OrganizeAsync(RootPath ?? string.Empty, runId, dryRun, PruneEmptyFolders, cancellationToken);
}
