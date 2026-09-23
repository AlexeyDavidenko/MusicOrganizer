namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Result of <see cref="AudioTagsEncodingFixer.Propose"/>: the tags with any confident encoding
/// corrections applied, which fields were actually changed, and whether anything looked
/// suspicious but couldn't be confidently fixed.
/// </summary>
/// <param name="FixedTags">Tags with corrected values for any field a fix was found for.</param>
/// <param name="FixedFields">Names of the <see cref="AudioTags"/> properties that were corrected.</param>
/// <param name="NeedsManualReview">
/// True when at least one eligible field contained non-ASCII bytes but no candidate encoding won
/// confidently - left untouched rather than guessed, per "не изменять корректные данные".
/// </param>
public sealed record EncodingFixProposal(AudioTags FixedTags, IReadOnlyList<string> FixedFields, bool NeedsManualReview);
