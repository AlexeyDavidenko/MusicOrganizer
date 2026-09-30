using FluentAssertions;
using MusicOrganizer.Shared;

namespace MusicOrganizer.Tests.Unit.Shared;

public class TeeTextWriterTests
{
    [Fact]
    public void WriteLine_DuplicatesContentToEveryWriter()
    {
        var first = new StringWriter();
        var second = new StringWriter();
        var sut = new TeeTextWriter(first, second);

        sut.WriteLine("hello");
        sut.WriteLine("world");

        first.ToString().Should().Be(second.ToString());
        first.ToString().Should().Be($"hello{Environment.NewLine}world{Environment.NewLine}");
    }

    [Fact]
    public void Encoding_ReturnsFirstWriterEncoding()
    {
        var first = new StringWriter();
        var second = new StringWriter();
        var sut = new TeeTextWriter(first, second);

        sut.Encoding.Should().Be(first.Encoding);
    }
}
