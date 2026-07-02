namespace MusicOrganizer.Domain.TagRecovery;

/// <summary>
/// Result of proposing a tag recovery for one file: the tags as they would be after filling in
/// any missing fields, and which fields were actually filled.
/// </summary>
/// <param name="MergedTags">Tags after filling in any recovered fields.</param>
/// <param name="RecoveredFields">Names of the fields that were actually filled in.</param>
public sealed record TagRecoveryProposal(AudioTags MergedTags, IReadOnlyList<string> RecoveredFields);
