using System.Text.Json;
using FluentAssertions;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Infrastructure.Reporting;

namespace MusicOrganizer.Tests.Unit.Infrastructure.Reporting;

public class JsonReportWriterTests
{
    private readonly JsonReportWriter _sut = new();

    [Fact]
    public async Task WriteAsync_SerializesTitleSummaryColumnsAndRows()
    {
        var document = new ReportDocument
        {
            Title = "Scan",
            Columns = ["Status", "FilePath"],
            Rows = [new ReportRow(["OK", "/music/a.mp3"]), new ReportRow(["ERROR", "/music/b.mp3"])],
            Summary = ["Scanned 2 file(s): 1 with tags, 1 error(s)."],
        };

        using var stream = new MemoryStream();
        await _sut.WriteAsync(document, stream, CancellationToken.None);
        stream.Position = 0;

        using var parsed = await JsonDocument.ParseAsync(stream);
        var root = parsed.RootElement;

        root.GetProperty("title").GetString().Should().Be("Scan");
        root.GetProperty("summary")[0].GetString().Should().Be("Scanned 2 file(s): 1 with tags, 1 error(s).");
        root.GetProperty("columns").EnumerateArray().Select(e => e.GetString()).Should().Equal("Status", "FilePath");

        var rows = root.GetProperty("rows").EnumerateArray().ToList();
        rows.Should().HaveCount(2);
        rows[0].EnumerateArray().Select(e => e.GetString()).Should().Equal("OK", "/music/a.mp3");
        rows[1].EnumerateArray().Select(e => e.GetString()).Should().Equal("ERROR", "/music/b.mp3");
    }

    [Fact]
    public async Task WriteAsync_NullValue_SerializesAsJsonNull()
    {
        var document = new ReportDocument
        {
            Title = "Rename",
            Columns = ["Status", "Error"],
            Rows = [new ReportRow(["RENAMED", null])],
        };

        using var stream = new MemoryStream();
        await _sut.WriteAsync(document, stream, CancellationToken.None);
        stream.Position = 0;

        using var parsed = await JsonDocument.ParseAsync(stream);
        var row = parsed.RootElement.GetProperty("rows")[0];

        row[0].GetString().Should().Be("RENAMED");
        row[1].ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Format_IsJson()
    {
        _sut.Format.Should().Be(ReportFormat.Json);
    }
}
