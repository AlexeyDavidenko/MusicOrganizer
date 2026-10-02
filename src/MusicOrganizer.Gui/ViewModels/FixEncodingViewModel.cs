using MusicOrganizer.Application.Encoding;
using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="EncodingFixService"/> — the <c>fix-encoding</c> command.
/// </summary>
public sealed partial class FixEncodingViewModel : RootPathOperationViewModelBase<EncodingFixOutcome>
{
    private readonly EncodingFixService _encodingFixService;

    /// <summary>
    /// Creates a new <see cref="FixEncodingViewModel"/>.
    /// </summary>
    /// <param name="encodingFixService">Use case this screen wraps.</param>
    public FixEncodingViewModel(EncodingFixService encodingFixService)
    {
        _encodingFixService = encodingFixService;
    }

    /// <inheritdoc/>
    protected override IAsyncEnumerable<EncodingFixOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _encodingFixService.FixAsync(RootPath ?? string.Empty, runId, dryRun, cancellationToken);
}
