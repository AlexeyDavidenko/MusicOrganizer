using System.Text;
using FluentAssertions;
using MusicOrganizer.Domain;
using MusicOrganizer.Domain.Encoding;

namespace MusicOrganizer.Tests.Unit.Domain;

public class AudioTagsEncodingFixerTests
{
    static AudioTagsEncodingFixerTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Fact]
    public void Propose_FixesOnlyEligibleFields()
    {
        var mojibakeArtist = Mojibake("Максим Фадеев", 1251);
        var mojibakeAlbum = Mojibake("Лучшее", 1251);
        var tags = TagsFor(artist: mojibakeArtist, title: "Already Fine", album: mojibakeAlbum, genre: null);
        var eligibility = new TextFieldEligibility(
            TitleIsLatin1Sourced: true,
            ArtistIsLatin1Sourced: true,
            AlbumIsLatin1Sourced: false, // e.g. sourced from a UTF-8-declared ID3v2 frame
            GenreIsLatin1Sourced: true);

        var result = AudioTagsEncodingFixer.Propose(tags, eligibility);

        result.FixedFields.Should().Equal("Artist");
        result.FixedTags.Artist.Should().Be("Максим Фадеев");
        result.FixedTags.Album.Should().Be(mojibakeAlbum); // ineligible - left untouched even though it's fixable
        result.FixedTags.Title.Should().Be("Already Fine");
        result.NeedsManualReview.Should().BeFalse();
    }

    [Fact]
    public void Propose_FlagsManualReview_WhenEligibleFieldLooksSuspiciousButIsNotConfidentlyFixed()
    {
        var ambiguous = Mojibake("Ддт", 866);
        var tags = TagsFor(artist: ambiguous, title: null, album: null, genre: null);
        var eligibility = new TextFieldEligibility(false, true, false, false);

        var result = AudioTagsEncodingFixer.Propose(tags, eligibility);

        result.FixedFields.Should().BeEmpty();
        result.FixedTags.Artist.Should().Be(ambiguous);
        result.NeedsManualReview.Should().BeTrue();
    }

    [Fact]
    public void Propose_DoesNothing_WhenNoFieldsAreEligible()
    {
        var mojibakeArtist = Mojibake("Максим Фадеев", 1251);
        var tags = TagsFor(artist: mojibakeArtist, title: null, album: null, genre: null);
        var eligibility = new TextFieldEligibility(false, false, false, false);

        var result = AudioTagsEncodingFixer.Propose(tags, eligibility);

        result.FixedFields.Should().BeEmpty();
        result.NeedsManualReview.Should().BeFalse();
        result.FixedTags.Should().Be(tags);
    }

    [Fact]
    public void Propose_DoesNothing_WhenEligibleFieldIsNull()
    {
        var tags = TagsFor(artist: null, title: null, album: null, genre: null);
        var eligibility = new TextFieldEligibility(true, true, true, true);

        var result = AudioTagsEncodingFixer.Propose(tags, eligibility);

        result.FixedFields.Should().BeEmpty();
        result.NeedsManualReview.Should().BeFalse();
    }

    private static string Mojibake(string real, int codePage) =>
        Encoding.Latin1.GetString(Encoding.GetEncoding(codePage).GetBytes(real));

    private static AudioTags TagsFor(string? artist, string? title, string? album, string? genre) =>
        new(Title: title, Artist: artist, Album: album, Year: null, TrackNumber: null, Genre: genre);
}
