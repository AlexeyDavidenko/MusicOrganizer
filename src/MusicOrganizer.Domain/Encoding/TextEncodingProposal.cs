namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Result of running <see cref="TextEncodingDetector"/> against a single text value.
/// </summary>
/// <param name="CorrectedText">
/// The re-decoded text, when a confident correction was found; <see langword="null"/> when the
/// text is already correct or too ambiguous to safely change.
/// </param>
/// <param name="HadNonAsciiBytes">
/// True when the original text contained non-ASCII bytes at all (regardless of whether a
/// confident fix was found) - used to flag a field for manual review when it looks suspicious but
/// no candidate encoding won confidently.
/// </param>
public sealed record TextEncodingProposal(string? CorrectedText, bool HadNonAsciiBytes);
