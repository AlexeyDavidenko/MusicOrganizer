using System.Text;

namespace MusicOrganizer.Shared;

/// <summary>
/// Duplicates every write across multiple underlying <see cref="TextWriter"/> instances (for
/// example, the console and a report file), so callers can write once and have the same content
/// reach every destination without formatting it twice.
/// </summary>
public sealed class TeeTextWriter : TextWriter
{
    private readonly TextWriter[] _writers;

    /// <summary>
    /// Creates a writer that forwards every write to each of the given <paramref name="writers"/>,
    /// in order.
    /// </summary>
    /// <param name="writers">Destinations to duplicate writes to.</param>
    public TeeTextWriter(params TextWriter[] writers)
    {
        _writers = writers;
    }

    /// <inheritdoc/>
    public override Encoding Encoding => _writers[0].Encoding;

    /// <inheritdoc/>
    public override void Write(char value)
    {
        foreach (var writer in _writers)
        {
            writer.Write(value);
        }
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
        foreach (var writer in _writers)
        {
            writer.Write(value);
        }
    }

    /// <inheritdoc/>
    public override void WriteLine(string? value)
    {
        foreach (var writer in _writers)
        {
            writer.WriteLine(value);
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        foreach (var writer in _writers)
        {
            writer.Flush();
        }
    }
}
