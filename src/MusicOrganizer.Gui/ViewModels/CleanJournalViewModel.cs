using CommunityToolkit.Mvvm.ComponentModel;
using MusicOrganizer.Application.Journal;
using MusicOrganizer.Domain.Journal;

namespace MusicOrganizer.Gui.ViewModels;

/// <summary>
/// Screen for <see cref="CleanJournalUseCase"/> — the <c>clean-journal</c> command. Takes an age
/// cutoff rather than a root folder, so it extends <see cref="StreamingOperationViewModelBase{TOutcome}"/>
/// directly instead of <see cref="RootPathOperationViewModelBase{TOutcome}"/>.
/// </summary>
public sealed partial class CleanJournalViewModel : StreamingOperationViewModelBase<JournalCleanupOutcome>
{
    private readonly CleanJournalUseCase _cleanJournalUseCase;

    /// <summary>
    /// Creates a new <see cref="CleanJournalViewModel"/>.
    /// </summary>
    /// <param name="cleanJournalUseCase">Use case this screen wraps.</param>
    public CleanJournalViewModel(CleanJournalUseCase cleanJournalUseCase)
    {
        _cleanJournalUseCase = cleanJournalUseCase;
    }

    [ObservableProperty]
    public partial int OlderThanDays { get; set; } = 30;

    /// <inheritdoc/>
    protected override IAsyncEnumerable<JournalCleanupOutcome> ExecuteAsync(Guid runId, bool dryRun, CancellationToken cancellationToken) =>
        _cleanJournalUseCase.CleanAsync(TimeSpan.FromDays(OlderThanDays), dryRun, cancellationToken);
}
