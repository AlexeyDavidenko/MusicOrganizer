using MusicOrganizer.Application.Recovery;
using MusicOrganizer.Domain.TagRecovery;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="TagRecoveryService"/> — the <c>recover-tags</c> command.
/// </summary>
public sealed partial class RecoverTagsViewModel : RootPathOperationViewModelBase<TagRecoveryOutcome>
{
    private readonly TagRecoveryService _tagRecoveryService;

    /// <summary>
    /// Creates a new <see cref="RecoverTagsViewModel"/>.
    /// </summary>
    /// <param name="tagRecoveryService">Use case this screen wraps.</param>
    public RecoverTagsViewModel(TagRecoveryService tagRecoveryService)
    {
        _tagRecoveryService = tagRecoveryService;
    }

    /// <inheritdoc/>
    protected override IAsyncEnumerable<TagRecoveryOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _tagRecoveryService.RecoverAsync(RootPath ?? string.Empty, runId, dryRun, cancellationToken);
}
