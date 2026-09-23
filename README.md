# MusicOrganizer

[![CI](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml/badge.svg)](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/AlexeyDavidenko/MusicOrganizer)](https://github.com/AlexeyDavidenko/MusicOrganizer/releases)

A cross-platform .NET 10 CLI for maintaining large MP3 collections — from a few thousand tracks
up to 1,000,000+ files. It recovers missing tags, renames files consistently, finds and removes
duplicates, and makes every change safe to undo.

Every operation that touches disk supports **dry-run**, is recorded in a **journal**, and can be
**rolled back** — nothing is applied irreversibly by default.

## Features

- **Scan** a collection and read ID3 tags (`scan`).
- **Recover missing Artist/Title/Album tags** through an 8-level priority chain — ID3, file name
  parsing, folder structure (Album/Artist/Parent), collection-wide statistics (consensus across
  sibling files), extended filename heuristics (track numbers, noise suffixes), and a
  manual-review report for anything no source could resolve (`recover-tags`).
- **Rename files** to a consistent `Artist-Title.mp3` template, with Unicode normalization,
  forbidden-character stripping, deterministic collision resolution, and optional Cyrillic → Latin
  transliteration (`rename`, `--transliterate`, BGN/PCGN).
- **Organize the collection** into an `Artist/Album` folder tree (falling back to a flat
  `Artist/` folder when Album is unknown), leaving file names untouched — composes with `rename`
  in either order (`organize`).
- **Find duplicates** — exact content matches (size + hash) and probable duplicates by matching
  tags (`find-duplicates`, read-only).
- **Remove exact duplicates**, always keeping the file with the shortest path in each group;
  tag-only matches are never touched automatically, since they may be legitimate alternate
  versions (`remove-duplicates`).
- **Roll back** any previous `--apply` run by its run id, restoring modified or deleted files from
  the journal (`rollback`).
- Every mutating command is dry-run by default; per-file errors never abort a run on the rest of
  the collection.

## Non-goals (for now)

Encoding auto-detection (Windows-1251/CP866/etc. — currently delegated to TagLibSharp), file-based
reports, and audio fingerprinting are not implemented yet. The architecture is designed to add them, along with other
audio formats (FLAC, OGG, AAC, ...) and online metadata sources (MusicBrainz, AcoustID, Discogs,
Last.fm), without restructuring existing code.

## Technology

.NET 10 · C# · [Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host) ·
[System.CommandLine](https://github.com/dotnet/command-line-api) ·
[TagLibSharp](https://github.com/mono/taglib-sharp) · xUnit · FluentAssertions

## Architecture

Clean Architecture across six projects, with dependencies flowing strictly one way:

```
src/
  MusicOrganizer.Domain/           — entities, value objects, domain interfaces (no I/O)
  MusicOrganizer.Application/      — use cases, ports implemented by Infrastructure
  MusicOrganizer.Infrastructure/   — TagLibSharp, file system, journal/rollback storage
  MusicOrganizer.Shared/           — cross-cutting primitives/utilities
  MusicOrganizer.Cli/              — composition root: Generic Host + System.CommandLine
tests/
  MusicOrganizer.Tests/            — Unit/ and Integration/
```

`Domain` and `Shared` depend on nothing else in the solution. `Application` depends only on
`Domain`/`Shared`. `Infrastructure` implements the ports `Application` defines. `Cli` is the only
place all layers are wired together via dependency injection.

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/MusicOrganizer.Cli -- --help
```

## Usage

```bash
# Scan: recursively finds *.mp3, reads tags, prints a report
dotnet run --project src/MusicOrganizer.Cli -- scan /path/to/music

# Recover missing tags — dry-run by default, --apply to actually write
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music --apply

# Rename to Artist-Title.mp3
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply

# Same, with Cyrillic transliterated to Latin (BGN/PCGN)
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply --transliterate

# Organize into an Artist/Album folder tree (file names are left as-is)
dotnet run --project src/MusicOrganizer.Cli -- organize /path/to/music --apply

# Find duplicates (read-only)
dotnet run --project src/MusicOrganizer.Cli -- find-duplicates /path/to/music

# Remove exact duplicates — dry-run by default, --apply to actually delete
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music --apply

# Undo a previous --apply run (recover-tags/rename/organize/remove-duplicates print a run id)
dotnet run --project src/MusicOrganizer.Cli -- rollback <run-id>
```

## Docker

```bash
docker build -t musicorganizer .
docker run --rm -v /path/to/music:/music musicorganizer scan /music
```

Multi-stage image on official `mcr.microsoft.com/dotnet` base images, runs as a non-root user,
supports `linux/amd64` and `linux/arm64`. Images for `main` are published to GHCR
(`ghcr.io/alexeydavidenko/musicorganizer`) on every push; tagged releases also publish
self-contained single-file binaries for Windows, Linux, and macOS (x64/arm64).

## Development

Before committing: `dotnet format --verify-no-changes`, `dotnet build` (zero warnings,
`TreatWarningsAsErrors` enabled), `dotnet test`. CI (GitHub Actions) runs the same checks on
Linux, Windows, and macOS for every pull request.

## Project status

Under active development, first release `v0.1.0` published. Implemented: 6 commands (`scan`,
`recover-tags`, `rename`, `organize`, `find-duplicates`, `remove-duplicates`) plus a shared
journal/rollback mechanism, all 8 tag recovery levels, resilient scanning (an unreadable folder is
skipped and logged instead of aborting the run), 110 passing tests, and a CI/CD pipeline that
builds a multi-arch Docker image and publishes tagged releases with prebuilt binaries. See the
"Non-goals" section above for what's still missing.

## License

Not yet decided.
