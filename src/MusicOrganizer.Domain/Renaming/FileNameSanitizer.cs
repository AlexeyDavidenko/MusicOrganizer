using System.Text;
using System.Text.RegularExpressions;

namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Turns arbitrary tag text into a safe file name component: normalizes Unicode, replaces
/// whitespace with underscores, strips characters that are invalid (or dangerous, e.g. path
/// separators) in a file name, collapses repeated separators, and caps length.
/// </summary>
public static partial class FileNameSanitizer
{
    private const int MaxComponentLength = 100;
    private static readonly char[] ForbiddenCharacters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// Sanitizes a single file name component (e.g. an Artist or Title value).
    /// </summary>
    /// <param name="value">Raw text to sanitize.</param>
    public static string SanitizeComponent(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormC).Trim();
        var withUnderscores = WhitespaceRunPattern().Replace(normalized, "_");

        var builder = new StringBuilder(withUnderscores.Length);
        foreach (var character in withUnderscores)
        {
            if (!char.IsControl(character) && Array.IndexOf(ForbiddenCharacters, character) < 0)
            {
                builder.Append(character);
            }
        }

        var collapsed = RepeatedSeparatorPattern().Replace(builder.ToString(), "$1");
        var trimmed = collapsed.Trim('_', '-', '.', ' ');

        return trimmed.Length > MaxComponentLength ? trimmed[..MaxComponentLength] : trimmed;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunPattern();

    [GeneratedRegex(@"([_-])\1+")]
    private static partial Regex RepeatedSeparatorPattern();
}
