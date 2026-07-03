using System.Security.Cryptography;
using MusicOrganizer.Application.Deduplication;

namespace MusicOrganizer.Infrastructure.Deduplication;

/// <summary>
/// Inspects files on the local file system for duplicate detection: file size via metadata, and
/// a SHA-256 content hash streamed from disk (never loads a whole file into memory).
/// </summary>
public sealed class FileContentInspector : IDuplicateFileInspector
{
    /// <inheritdoc />
    public Task<long> GetSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new FileInfo(filePath).Length);
    }

    /// <inheritdoc />
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hashBytes);
    }
}
