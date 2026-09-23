namespace MusicOrganizer.Domain.Encoding;

/// <summary>
/// Applies <see cref="TextEncodingDetector"/> across the text fields of an <see cref="AudioTags"/>
/// that are eligible for it, per <see cref="TextFieldEligibility"/>.
/// </summary>
public static class AudioTagsEncodingFixer
{
    /// <summary>
    /// Proposes encoding corrections for <paramref name="current"/>'s eligible text fields.
    /// </summary>
    /// <param name="current">Tags as currently read from the file.</param>
    /// <param name="eligibility">Which fields' current values are eligible for re-decoding.</param>
    public static EncodingFixProposal Propose(AudioTags current, TextFieldEligibility eligibility)
    {
        var title = Evaluate(current.Title, eligibility.TitleIsLatin1Sourced);
        var artist = Evaluate(current.Artist, eligibility.ArtistIsLatin1Sourced);
        var album = Evaluate(current.Album, eligibility.AlbumIsLatin1Sourced);
        var genre = Evaluate(current.Genre, eligibility.GenreIsLatin1Sourced);

        var fixedFields = new List<string>();
        if (title.WasFixed)
        {
            fixedFields.Add(nameof(AudioTags.Title));
        }

        if (artist.WasFixed)
        {
            fixedFields.Add(nameof(AudioTags.Artist));
        }

        if (album.WasFixed)
        {
            fixedFields.Add(nameof(AudioTags.Album));
        }

        if (genre.WasFixed)
        {
            fixedFields.Add(nameof(AudioTags.Genre));
        }

        var needsManualReview = title.NeedsManualReview || artist.NeedsManualReview
            || album.NeedsManualReview || genre.NeedsManualReview;

        var fixedTags = current with { Title = title.Value, Artist = artist.Value, Album = album.Value, Genre = genre.Value };
        return new EncodingFixProposal(fixedTags, fixedFields, needsManualReview);
    }

    private static FieldEvaluation Evaluate(string? value, bool isEligible)
    {
        if (!isEligible || value is null)
        {
            return new FieldEvaluation(value, WasFixed: false, NeedsManualReview: false);
        }

        var proposal = TextEncodingDetector.Detect(value);
        return proposal.CorrectedText is not null
            ? new FieldEvaluation(proposal.CorrectedText, WasFixed: true, NeedsManualReview: false)
            : new FieldEvaluation(value, WasFixed: false, proposal.HadNonAsciiBytes);
    }

    private readonly record struct FieldEvaluation(string? Value, bool WasFixed, bool NeedsManualReview);
}
