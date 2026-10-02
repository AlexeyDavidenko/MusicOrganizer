# MusicOrganizer

[![CI](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml/badge.svg)](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/AlexeyDavidenko/MusicOrganizer)](https://github.com/AlexeyDavidenko/MusicOrganizer/releases)

A cross-platform .NET 10 tool for maintaining large MP3 collections — from a few thousand tracks
up to 1,000,000+ files. It recovers missing tags, renames files consistently, finds and removes
duplicates, and makes every change safe to undo. Available as a CLI and as a desktop GUI
(Avalonia, Windows/Linux/macOS) with full feature parity.

Every operation that touches disk supports **dry-run**, is recorded in a **journal**, and can be
**rolled back** — nothing is applied irreversibly by default.

## Features

- **Scan** a collection and read ID3 tags (`scan`).
- **Recover missing Artist/Title/Album tags** through an 8-level priority chain — ID3, file name
  parsing, folder structure (Album/Artist/Parent), collection-wide statistics (consensus across
  sibling files), extended filename heuristics (track numbers, noise suffixes), and a
  manual-review report for anything no source could resolve (`recover-tags`).
- **Write a plain-text report file** alongside the console output for `scan`, `recover-tags`, and
  `find-duplicates` (`--report <path>`, overwritten each run).
- **Rename files** to a consistent `Artist-Title.mp3` template, with Unicode normalization,
  forbidden-character stripping, deterministic collision resolution, and optional Cyrillic → Latin
  transliteration (`rename`, `--transliterate`, BGN/PCGN).
- **Organize the collection** into an `Artist/Album` folder tree (falling back to a flat
  `Artist/` folder when Album is unknown), leaving file names untouched — composes with `rename`
  in either order (`organize`), optionally removing folders left empty by the move
  (`--prune-empty-folders`, opt-in, fully reversible via `rollback`).
- **Detect and correct mis-decoded tag text** — legacy Cyrillic encodings (Windows-1251, CP866)
  or UTF-8 read as Latin1, and Windows-1252 punctuation read as Latin1 control characters,
  restored via reversible byte re-decoding with confidence-gated detection; genuinely correct
  text is left untouched (`fix-encoding`).
- **Find duplicates** — exact content matches (size + hash) and probable duplicates by matching
  tags (`find-duplicates`, read-only).
- **Remove exact duplicates**, always keeping the file with the shortest path in each group;
  tag-only matches are never touched automatically, since they may be legitimate alternate
  versions (`remove-duplicates`).
- **Roll back** any previous `--apply` run by its run id, restoring modified or deleted files (and
  any folders removed by `--prune-empty-folders`) from the journal, in reverse-chronological order
  so dependent changes undo correctly (`rollback`).
- **Prune old journal runs** so backups don't accumulate forever (`clean-journal
  [--older-than-days N]`, dry-run by default, 30 days by default).
- Every mutating command is dry-run by default; per-file errors never abort a run on the rest of
  the collection.
- **Desktop GUI** (Avalonia) with every command above as its own screen, plus a "Save results…"
  export and a run picker for `rollback` so you don't have to copy a run id by hand.

## Non-goals (for now)

Structured report formats (JSON/CSV/HTML — only plain text exists today) and audio fingerprinting
are not implemented yet. The architecture is designed to add them, along with other audio formats
(FLAC, OGG, AAC, ...) and online metadata sources (MusicBrainz, AcoustID, Discogs, Last.fm),
without restructuring existing code.

## Technology

.NET 10 · C# · [Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host) ·
[System.CommandLine](https://github.com/dotnet/command-line-api) ·
[TagLibSharp](https://github.com/mono/taglib-sharp) · xUnit · FluentAssertions

## Architecture

Clean Architecture across seven projects, with dependencies flowing strictly one way:

```
src/
  MusicOrganizer.Domain/           — entities, value objects, domain interfaces (no I/O)
  MusicOrganizer.Application/      — use cases, ports implemented by Infrastructure
  MusicOrganizer.Infrastructure/   — TagLibSharp, file system, journal/rollback storage
  MusicOrganizer.Shared/           — cross-cutting primitives/utilities
  MusicOrganizer.Cli/              — composition root: Generic Host + System.CommandLine
  MusicOrganizer.Gui/              — composition root: Generic Host + Avalonia (MVVM)
tests/
  MusicOrganizer.Tests/            — Unit/, Integration/, and Architecture/
```

`Domain` and `Shared` depend on nothing else in the solution. `Application` depends only on
`Domain`/`Shared`. `Infrastructure` implements the ports `Application` defines. `Cli` and `Gui` are
the only places all layers are wired together via dependency injection — both call the exact same
`AddApplicationServices()`/`AddInfrastructureServices()`, so every feature lives in `Application`/
`Infrastructure` exactly once regardless of which front end uses it. These rules are enforced by
both the compiler (`ProjectReference`s only go one way) and dedicated architecture tests
(`NetArchTest`).

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

# Same, also writing a plain-text report file
dotnet run --project src/MusicOrganizer.Cli -- scan /path/to/music --report scan-report.txt

# Recover missing tags — dry-run by default, --apply to actually write
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music --apply --report recover-report.txt

# Detect and correct mis-decoded tag text — dry-run by default, --apply to actually write
dotnet run --project src/MusicOrganizer.Cli -- fix-encoding /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- fix-encoding /path/to/music --apply

# Rename to Artist-Title.mp3
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply

# Same, with Cyrillic transliterated to Latin (BGN/PCGN)
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply --transliterate

# Organize into an Artist/Album folder tree (file names are left as-is)
dotnet run --project src/MusicOrganizer.Cli -- organize /path/to/music --apply

# Same, also removing folders left empty by the move
dotnet run --project src/MusicOrganizer.Cli -- organize /path/to/music --apply --prune-empty-folders

# Find duplicates (read-only), optionally also writing a plain-text report file
dotnet run --project src/MusicOrganizer.Cli -- find-duplicates /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- find-duplicates /path/to/music --report duplicates-report.txt

# Remove exact duplicates — dry-run by default, --apply to actually delete
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music --apply

# Undo a previous --apply run (recover-tags/fix-encoding/rename/organize/remove-duplicates print a run id)
dotnet run --project src/MusicOrganizer.Cli -- rollback <run-id>

# Delete journal runs older than 30 days (dry-run by default)
dotnet run --project src/MusicOrganizer.Cli -- clean-journal
dotnet run --project src/MusicOrganizer.Cli -- clean-journal --older-than-days 7 --apply
```

## Desktop GUI

```bash
dotnet run --project src/MusicOrganizer.Gui
```

A sidebar lists all 9 commands; each screen mirrors its CLI counterpart's options (dry-run/apply
toggle, extra flags like `--transliterate` or `--prune-empty-folders`) and streams results into a
list as they're processed. Every screen has a "Save results…" button to write what's shown to a
plain-text file. `rollback` additionally lists recent runs from the journal to pick from, instead
of requiring the run id to be pasted in by hand.

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
`TreatWarningsAsErrors` enabled, `StyleCop.Analyzers` included), `dotnet test`. CI (GitHub Actions)
runs the same checks on Linux, Windows, and macOS for every pull request.

A ready-to-use [Dev Container](.devcontainer/devcontainer.json) is provided for VS Code and
JetBrains Rider (both read the same file natively) — open the repo and reopen in container to get
the .NET SDK, `git`, `gh`, and `docker` preinstalled, with the solution restored automatically.

## Project status

Under active development, first release `v0.1.0` published. Implemented: 8 commands (`scan`,
`recover-tags`, `fix-encoding`, `rename`, `organize`, `find-duplicates`, `remove-duplicates`,
`clean-journal`) plus a shared journal/rollback mechanism, all 8 tag recovery levels, encoding
detection/correction, resilient scanning (an unreadable folder is skipped and logged instead of
aborting the run), reversible empty-folder pruning after `organize`, plain-text file reports for
`scan`/`recover-tags`/`find-duplicates` (`--report`), a desktop GUI with full command parity, 165
passing tests, and a CI/CD pipeline that builds a multi-arch Docker image and publishes tagged
releases with prebuilt binaries. See the "Non-goals" section above for what's still missing.

## License

Not yet decided.
