namespace MusicOrganizer.Tests.TestSupport;

/// <summary>
/// Builds a small but structurally valid MPEG-1 Layer III file — just enough for TagLibSharp to
/// recognize it as an MP3 and locate a place to write/read ID3 tags. The audio content is silent
/// filler, not real audio; tests only exercise tag reading, never playback.
/// </summary>
internal static class MinimalMp3Fixture
{
    // MPEG-1, Layer III, no CRC, 128 kbps, 44100 Hz, mono, no padding/private/copyright/emphasis.
    private static readonly byte[] FrameHeader = [0xFF, 0xFB, 0x90, 0xC0];

    // 144 * 128000 / 44100, rounded down, including the 4-byte header.
    private const int FrameSize = 417;
    private const int FrameCount = 60;

    /// <summary>
    /// Writes a minimal valid MP3 file to <paramref name="filePath"/>.
    /// </summary>
    public static void CreateAt(string filePath)
    {
        var bytes = new byte[FrameSize * FrameCount];
        for (var frame = 0; frame < FrameCount; frame++)
        {
            FrameHeader.CopyTo(bytes, frame * FrameSize);
        }

        File.WriteAllBytes(filePath, bytes);
    }
}
