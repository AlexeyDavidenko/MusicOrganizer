using CommunityToolkit.Mvvm.ComponentModel;
using MusicOrganizer.Application.Renaming;
using MusicOrganizer.Domain.Renaming;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="RenameEngine"/> — the <c>rename</c> command.
/// </summary>
public sealed partial class RenameViewModel : RootPathOperationViewModelBase<RenameOutcome>
{
    private readonly RenameEngine _renameEngine;

    /// <summary>
    /// Creates a new <see cref="RenameViewModel"/>.
    /// </summary>
    /// <param name="renameEngine">Use case this screen wraps.</param>
    public RenameViewModel(RenameEngine renameEngine)
    {
        _renameEngine = renameEngine;
    }

    [ObservableProperty]
    public partial bool Transliterate { get; set; }

    /// <inheritdoc/>
    protected override IAsyncEnumerable<RenameOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _renameEngine.RenameAsync(RootPath ?? string.Empty, runId, dryRun, Transliterate, cancellationToken);
}
