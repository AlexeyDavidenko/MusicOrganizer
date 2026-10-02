using System.Text;
using SystemTextEncoding = System.Text.Encoding;

namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Detects whether a text value that was decoded as Latin1 (ID3v1 fields, and ID3v2 text frames
/// whose declared encoding byte is Latin1, both decode this way - see PROMPT.md's "MP3 SUPPORT"
/// encoding list) was actually raw bytes in one of a fixed set of other encodings, and re-decodes
/// it correctly. Pure - no IO, no TagLibSharp dependency; only <see cref="System.Text.Encoding"/>
/// and <see cref="CodePagesEncodingProvider"/> (a data-table provider, not an infrastructure
/// concern) are used.
/// </summary>
/// <remarks>
/// Latin1 decoding is exactly reversible: each character in a Latin1-decoded string has a
/// codepoint equal to one original byte (0-255). That means the original raw bytes can be
/// reconstructed from the string alone, without ever touching the file again, and re-decoded
/// against each of PROMPT.md's other listed encodings to see which one (if any) actually explains
/// the data. Genuinely correct text (plain ASCII, or real Western-European Latin1/Windows-1252
/// text) is left untouched - "не изменять корректные данные".
/// </remarks>
public static class TextEncodingDetector
{
    // Minimum average-letter-frequency score for a candidate to be considered plausibly real
    // Russian text at all (calibrated against real song/artist names scoring 2-7 vs. Western
    // Latin1 text scoring under 0.4 when wrongly run through the same scorer).
    private const double MinimumPlausibleCyrillicScore = 2.0;

    // Required ratio between the winning and losing candidate's score to pick one over the
    // other - protects short/atypical strings (e.g. a 3-letter acronym) where both candidates
    // can score close enough that picking either would be a guess, not a detection.
    private const double CyrillicDecisionMargin = 1.3;

    // Windows-1251 and CP866 both map the same raw bytes into characters that fall inside the
    // Cyrillic Unicode block, just different ones - a plain "is this a Cyrillic letter" check
    // scores ~1.0 for *both* candidates on real Cyrillic-source bytes and can't tell them apart
    // (verified empirically: e.g. CP866 bytes for "Максим Фадеев", decoded as Windows-1251
    // instead, land entirely on other Cyrillic-block letters). Scoring against real Russian
    // letter frequency instead - correct decodings land on common letters (о, е, а, и, н...),
    // wrong ones scatter roughly evenly across the whole alphabet - discriminates correctly.
    private static readonly IReadOnlyDictionary<char, double> RussianLetterFrequency = new Dictionary<char, double>
    {
        ['о'] = 10.97,
        ['е'] = 8.45,
        ['а'] = 8.01,
        ['и'] = 7.35,
        ['н'] = 6.70,
        ['т'] = 6.26,
        ['с'] = 5.47,
        ['р'] = 4.73,
        ['в'] = 4.54,
        ['л'] = 4.40,
        ['к'] = 3.49,
        ['м'] = 3.21,
        ['д'] = 2.98,
        ['п'] = 2.81,
        ['у'] = 2.62,
        ['я'] = 2.01,
        ['ы'] = 1.90,
        ['ь'] = 1.74,
        ['г'] = 1.70,
        ['з'] = 1.65,
        ['б'] = 1.59,
        ['ч'] = 1.44,
        ['й'] = 1.21,
        ['х'] = 0.97,
        ['ж'] = 0.94,
        ['ш'] = 0.73,
        ['ю'] = 0.64,
        ['ц'] = 0.48,
        ['щ'] = 0.36,
        ['э'] = 0.32,
        ['ф'] = 0.26,
        ['ъ'] = 0.04,
        ['ё'] = 0.04,
    };

    static TextEncodingDetector()
    {
        SystemTextEncoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Detects whether <paramref name="latin1DecodedText"/> is actually mis-decoded text in
    /// another encoding, and proposes a correction.
    /// </summary>
    /// <param name="latin1DecodedText">
    /// The text as currently decoded, assuming it came from a Latin1 (ID3v1, or ID3v2 declared
    /// Latin1) source.
    /// </param>
    /// <returns>
    /// A proposal with the corrected text if a better decoding was found, or <c>null</c> if the
    /// text looks genuinely correct as-is.
    /// </returns>
    public static TextEncodingProposal Detect(string latin1DecodedText)
    {
        if (IsAscii(latin1DecodedText))
        {
            return new TextEncodingProposal(CorrectedText: null, HadNonAsciiBytes: false);
        }

        var rawBytes = ToLatin1Bytes(latin1DecodedText);

        if (TryDecodeUtf8Strict(rawBytes, out var utf8Text) && LooksLikeRealText(utf8Text))
        {
            return new TextEncodingProposal(utf8Text, HadNonAsciiBytes: true);
        }

        var windows1251Text = Decode(rawBytes, 1251);
        var cp866Text = Decode(rawBytes, 866);
        var windows1251Score = ContainsControlCharacter(windows1251Text) ? 0.0 : RussianFrequencyScore(windows1251Text);
        var cp866Score = ContainsControlCharacter(cp866Text) ? 0.0 : RussianFrequencyScore(cp866Text);

        if (windows1251Score >= MinimumPlausibleCyrillicScore && windows1251Score >= cp866Score * CyrillicDecisionMargin)
        {
            return new TextEncodingProposal(windows1251Text, HadNonAsciiBytes: true);
        }

        if (cp866Score >= MinimumPlausibleCyrillicScore && cp866Score >= windows1251Score * CyrillicDecisionMargin)
        {
            return new TextEncodingProposal(cp866Text, HadNonAsciiBytes: true);
        }

        // Windows-1252 differs from strict Latin1 only in the 0x80-0x9F range, which Latin1
        // treats as unprintable C1 control codes and Windows-1252 treats as real printable
        // characters (curly quotes, dashes, ellipsis, etc.) - a literal control character in a
        // tag value is never intentional, so replacing it via Windows-1252 is a safe, narrow fix.
        // Gated tightly: every character must already be plain ASCII *except* for one or more
        // literal C1 control characters (0x80-0x9F) - i.e. "mostly-English text with a Windows
        // smart quote/dash/ellipsis in it", the actual real-world case this handles. Any other
        // non-ASCII character (e.g. Cyrillic-as-Latin1 bytes the checks above didn't confidently
        // resolve) disqualifies this branch - otherwise it would turn "don't know" into a wrong
        // guess instead of leaving the value alone (verified against a short Cyrillic acronym
        // that legitimately contains one 0x80-0x9F byte but is not Windows-1252 punctuation text).
        if (IsAsciiOutsideC1ControlRange(latin1DecodedText) && ContainsControlCharacter(latin1DecodedText))
        {
            var windows1252Text = Decode(rawBytes, 1252);
            if (LooksLikeRealText(windows1252Text))
            {
                return new TextEncodingProposal(windows1252Text, HadNonAsciiBytes: true);
            }
        }

        return new TextEncodingProposal(CorrectedText: null, HadNonAsciiBytes: true);
    }

    private static bool IsAscii(string text)
    {
        foreach (var character in text)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] ToLatin1Bytes(string text)
    {
        var bytes = new byte[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            bytes[i] = (byte)text[i];
        }

        return bytes;
    }

    private static bool TryDecodeUtf8Strict(byte[] bytes, out string text)
    {
        try
        {
            var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            text = strictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static string Decode(byte[] bytes, int codePage) => SystemTextEncoding.GetEncoding(codePage).GetString(bytes);

    private static bool LooksLikeRealText(string text) =>
        text.Length > 0 && !ContainsControlCharacter(text) && ContainsLetter(text);

    private static bool IsAsciiOutsideC1ControlRange(string text)
    {
        foreach (var character in text)
        {
            // 0x00-0x7F is ASCII; 0x80-0x9F is the C1 control range this branch exists to fix.
            // Anything above that (0xA0-0xFF) is genuine Latin1-supplement territory - disqualify.
            if (character > 0x9F)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsControlCharacter(string text)
    {
        foreach (var character in text)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsLetter(string text)
    {
        foreach (var character in text)
        {
            if (char.IsLetter(character))
            {
                return true;
            }
        }

        return false;
    }

    private static double RussianFrequencyScore(string text)
    {
        var letters = 0;
        var frequencySum = 0.0;
        foreach (var character in text)
        {
            if (!char.IsLetter(character))
            {
                continue;
            }

            letters++;
            frequencySum += RussianLetterFrequency.GetValueOrDefault(char.ToLowerInvariant(character));
        }

        return letters == 0 ? 0.0 : frequencySum / letters;
    }
}
