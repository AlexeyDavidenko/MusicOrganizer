using System.Text;
using FluentAssertions;
using MusicOrganizer.Application.Reporting;
using MusicOrganizer.Infrastructure.Reporting;

namespace MusicOrganizer.Tests.Unit.Infrastructure.Reporting;

public class HtmlReportWriterTests
{
    private readonly HtmlReportWriter _sut = new();

    [Fact]
    public async Task WriteAsync_IncludesTitleSummaryAndTable()
    {
        var document = new ReportDocument
        {
            Title = "Scan",
            Columns = ["Status", "FilePath"],
            Rows = [new ReportRow(["OK", "/music/a.mp3"])],
            Summary = ["Scanned 1 file(s)."],
        };

        var html = await WriteAndReadAsync(document);

        html.Should().Contain("<title>Scan</title>");
        html.Should().Contain("<h1>Scan</h1>");
        html.Should().Contain("<li>Scanned 1 file(s).</li>");
        html.Should().Contain("<th>Status</th>");
        html.Should().Contain("<th>FilePath</th>");
        html.Should().Contain("<td>OK</td>");
        html.Should().Contain("<td>/music/a.mp3</td>");
    }

    [Fact]
    public async Task WriteAsync_WithoutSummary_OmitsBulletList()
    {
        var document = new ReportDocument { Title = "Scan", Columns = ["Status"], Rows = [] };

        var html = await WriteAndReadAsync(document);

        html.Should().NotContain("<ul>");
    }

    [Fact]
    public async Task WriteAsync_HtmlEncodesValuesAndSummary()
    {
        var document = new ReportDocument
        {
            Title = "Fix Encoding",
            Columns = ["FilePath"],
            Rows = [new ReportRow(["<script>alert(1)</script>"])],
            Summary = ["Contains <b>markup</b> & ampersands"],
        };

        var html = await WriteAndReadAsync(document);

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
        html.Should().Contain("&amp;");
    }

    [Fact]
    public async Task WriteAsync_NullValue_RendersEmptyCell()
    {
        var document = new ReportDocument { Title = "T", Columns = ["Status", "Error"], Rows = [new ReportRow(["OK", null])] };

        var html = await WriteAndReadAsync(document);

        html.Should().Contain("<td>OK</td>");
        html.Should().Contain("<td></td>");
    }

    [Fact]
    public void Format_IsHtml()
    {
        _sut.Format.Should().Be(ReportFormat.Html);
    }

    private async Task<string> WriteAndReadAsync(ReportDocument document)
    {
        using var stream = new MemoryStream();
        await _sut.WriteAsync(document, stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
