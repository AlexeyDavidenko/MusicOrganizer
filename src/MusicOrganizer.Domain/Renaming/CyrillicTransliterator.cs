using System.Text;

namespace MusicOrganizer.Domain.Renaming;

/// <summary>
/// Transliterates Russian Cyrillic text to Latin using the BGN/PCGN romanization system.
/// Non-Cyrillic characters (Latin, digits, punctuation, other scripts) pass through unchanged. See
/// ADR-0009 for the scheme choice and the simplifications documented below.
/// </summary>
public static class CyrillicTransliterator
{
    private static readonly Dictionary<char, string> Map = new()
    {
        ['а'] = "a",
        ['б'] = "b",
        ['в'] = "v",
        ['г'] = "g",
        ['д'] = "d",
        ['ж'] = "zh",
        ['з'] = "z",
        ['и'] = "i",
        ['й'] = "y",
        ['к'] = "k",
        ['л'] = "l",
        ['м'] = "m",
        ['н'] = "n",
        ['о'] = "o",
        ['п'] = "p",
        ['р'] = "r",
        ['с'] = "s",
        ['т'] = "t",
        ['у'] = "u",
        ['ф'] = "f",
        ['х'] = "kh",
        ['ц'] = "ts",
        ['ч'] = "ch",
        ['ш'] = "sh",
        ['щ'] = "shch",
        ['ъ'] = "",
        ['ы'] = "y",
        ['ь'] = "",
        ['э'] = "e",
        ['ю'] = "yu",
        ['я'] = "ya",
    };

    private static readonly HashSet<char> Consonants =
        ['б', 'в', 'г', 'д', 'ж', 'з', 'й', 'к', 'л', 'м', 'н', 'п', 'р', 'с', 'т', 'ф', 'х', 'ц', 'ч', 'ш', 'щ'];

    /// <summary>
    /// Transliterates <paramref name="value"/>, leaving any non-Cyrillic character untouched.
    /// </summary>
    /// <param name="value">Text to transliterate.</param>
    public static string Transliterate(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            var lower = char.ToLowerInvariant(current);
            var isUpper = char.IsUpper(current);

            if (lower is 'е' or 'ё')
            {
                var previousLower = i > 0 ? char.ToLowerInvariant(value[i - 1]) : (char?)null;
                var startsWord = previousLower is null || !Consonants.Contains(previousLower.Value);
                builder.Append(ApplyCase(startsWord ? "ye" : "e", isUpper));
                continue;
            }

            if (Map.TryGetValue(lower, out var mapped))
            {
                builder.Append(ApplyCase(mapped, isUpper));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string ApplyCase(string mapped, bool isUpper)
    {
        if (mapped.Length == 0 || !isUpper)
        {
            return mapped;
        }

        return char.ToUpperInvariant(mapped[0]) + mapped[1..];
    }
}
