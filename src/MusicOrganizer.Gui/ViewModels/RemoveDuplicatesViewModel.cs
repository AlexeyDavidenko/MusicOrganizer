using MusicOrganizer.Application.Deduplication;
using MusicOrganizer.Domain.Deduplication;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="DuplicateRemovalService"/> — the <c>remove-duplicates</c> command.
/// </summary>
public sealed partial class RemoveDuplicatesViewModel : RootPathOperationViewModelBase<DuplicateRemovalOutcome>
{
    private readonly DuplicateRemovalService _duplicateRemovalService;

    /// <summary>
    /// Creates a new <see cref="RemoveDuplicatesViewModel"/>.
    /// </summary>
    /// <param name="duplicateRemovalService">Use case this screen wraps.</param>
    public RemoveDuplicatesViewModel(DuplicateRemovalService duplicateRemovalService)
    {
        _duplicateRemovalService = duplicateRemovalService;
    }

    /// <inheritdoc/>
    protected override IAsyncEnumerable<DuplicateRemovalOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _duplicateRemovalService.RemoveAsync(RootPath ?? string.Empty, runId, dryRun, cancellationToken);
}
