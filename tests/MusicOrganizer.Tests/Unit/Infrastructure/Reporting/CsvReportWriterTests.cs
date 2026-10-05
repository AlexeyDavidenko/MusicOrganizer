using System.Text;
using FluentAssertions;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Infrastructure.Reporting;

namespace MusicOrganizer.Tests.Unit.Infrastructure.Reporting;

public class CsvReportWriterTests
{
    private readonly CsvReportWriter _sut = new();

    [Fact]
    public async Task WriteAsync_WritesHeaderAndDataRows()
    {
        var document = new ReportDocument
        {
            Title = "Scan",
            Columns = ["Status", "FilePath"],
            Rows = [new ReportRow(["OK", "/music/a.mp3"]), new ReportRow(["ERROR", "/music/b.mp3"])],
            Summary = ["Scanned 2 file(s)."],
        };

        var text = await WriteAndReadAsync(document);

        text.Should().Be("Status,FilePath\r\nOK,/music/a.mp3\r\nERROR,/music/b.mp3\r\n");
    }

    [Fact]
    public async Task WriteAsync_OmitsTitleAndSummary()
    {
        var document = new ReportDocument
        {
            Title = "Scan",
            Columns = ["Status"],
            Rows = [new ReportRow(["OK"])],
            Summary = ["This prose must not appear in the CSV."],
        };

        var text = await WriteAndReadAsync(document);

        text.Should().NotContain("Scan");
        text.Should().NotContain("prose");
    }

    [Theory]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("a\"b", "\"a\"\"b\"")]
    [InlineData("a\nb", "\"a\nb\"")]
    [InlineData("plain", "plain")]
    public async Task WriteAsync_QuotesValuesThatNeedIt(string value, string expectedCell)
    {
        var document = new ReportDocument { Title = "T", Columns = ["Value"], Rows = [new ReportRow([value])] };

        var text = await WriteAndReadAsync(document);

        text.Should().Be($"Value\r\n{expectedCell}\r\n");
    }

    [Fact]
    public async Task WriteAsync_NullValue_RendersEmptyCell()
    {
        var document = new ReportDocument { Title = "T", Columns = ["Status", "Error"], Rows = [new ReportRow(["OK", null])] };

        var text = await WriteAndReadAsync(document);

        text.Should().Be("Status,Error\r\nOK,\r\n");
    }

    [Fact]
    public void Format_IsCsv()
    {
        _sut.Format.Should().Be(ReportFormat.Csv);
    }

    private async Task<string> WriteAndReadAsync(ReportDocument document)
    {
        using var stream = new MemoryStream();
        await _sut.WriteAsync(document, stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
