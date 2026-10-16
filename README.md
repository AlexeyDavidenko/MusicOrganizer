# MusicOrganizer

[![CI](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml/badge.svg)](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/AlexeyDavidenko/MusicOrganizer)](https://github.com/AlexeyDavidenko/MusicOrganizer/releases)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-informational)

**MusicOrganizer** is a cross-platform toolkit for cleaning up and maintaining large MP3
libraries — from a few thousand tracks to more than a million files. It recovers missing tags,
repairs mis-decoded text, renames and reorganizes files consistently, and finds and removes
duplicates, with every change recorded and fully reversible.

It ships as a command-line tool and as an Avalonia desktop application for Windows, Linux, and
macOS, both built on the same core so every feature behaves identically in either front end.

## Why MusicOrganizer

- **Safe by default.** Every mutating command runs as a dry run unless `--apply` is given. Applied
  changes are written to a journal and can be undone with a single `rollback <run-id>`.
- **Built for scale.** Collections are processed as a stream rather than loaded into memory, so
  libraries with 1,000,000+ files are a design target, not an edge case.
- **Fault-tolerant.** An unreadable folder or a corrupt file is logged and skipped; it never
  aborts the run for the rest of the collection.
- **Auditable.** Every command can export its results as plain text, JSON, CSV, or HTML.

## Features

| Command             | Purpose                                                                                    | Mutates files |
|---------------------|--------------------------------------------------------------------------------------------|:-------------:|
| `scan`              | Recursively find `*.mp3` files and read their ID3 tags                                     | No            |
| `recover-tags`      | Fill in missing Artist / Title / Album using an 8-level source chain                       | Yes           |
| `fix-encoding`      | Detect and correct mis-decoded tag text                                                    | Yes           |
| `rename`            | Rename files to a consistent `Artist-Title.mp3` template                                   | Yes           |
| `organize`          | Move files into an `Artist/Album` folder tree                                              | Yes           |
| `find-duplicates`   | Report exact (content) and probable (tag-based) duplicates                                 | No            |
| `remove-duplicates` | Delete exact duplicates, keeping one copy per group                                        | Yes           |
| `rollback`          | Undo a previous `--apply` run by its run id                                                | Yes           |
| `clean-journal`     | Prune old journal runs and their backups                                                   | Yes           |

### Tag recovery

`recover-tags` resolves each missing field through a prioritized chain of sources: existing ID3
data, file-name parsing, folder structure (Album / Artist / parent folder), collection-wide
statistics (consensus across sibling files), and extended file-name heuristics (track numbers,
noise suffixes). Anything no source can resolve with confidence is listed in a manual-review
report instead of being guessed.

### Encoding repair

`fix-encoding` detects text that was decoded with the wrong code page — legacy Cyrillic encodings
(Windows-1251, CP866) or UTF-8 read as Latin-1, and Windows-1252 punctuation read as Latin-1
control characters — and restores it through reversible byte re-decoding. Detection is
confidence-gated, so text that is already correct is left untouched.

### Renaming and organizing

`rename` applies Unicode normalization, strips characters forbidden by the file system, and
resolves name collisions deterministically. The optional `--transliterate` flag converts Cyrillic
to Latin using the BGN/PCGN standard.

`organize` moves files into `Artist/Album` folders (or a flat `Artist/` folder when the album is
unknown) without changing file names, so it composes with `rename` in either order. With
`--prune-empty-folders`, folders left empty by the move are removed — and restored by `rollback`.

### Duplicates

`find-duplicates` is read-only and reports two kinds of groups: exact matches (same size and
content hash) and probable matches (identical tags). `remove-duplicates` acts **only** on exact
matches, always keeping the file with the shortest path; tag-only matches are never deleted
automatically, since they are often legitimate alternate versions.

### Journal and rollback

Every `--apply` run prints a run id and records each change — including backups of modified and
deleted files — in a journal. `rollback <run-id>` reverts the run in reverse-chronological order,
so dependent changes (for example, a rename followed by a move) are undone correctly.
`clean-journal` removes runs older than a configurable age (30 days by default).

### Reports

`scan`, `recover-tags`, `fix-encoding`, `rename`, `organize`, `find-duplicates`, and
`remove-duplicates` accept `--report <path>` to write their results to a file, overwritten on each
run. `--format` selects the output format:

| Format            | Description                                                      |
|-------------------|------------------------------------------------------------------|
| `plain` (default) | Exact copy of the console output, written as the command runs    |
| `json`            | Structured document with title, rows, and summary                |
| `csv`             | Header plus data rows, ready for spreadsheets and scripts        |
| `html`            | Self-contained page with a results table                         |

## Installation

### Prebuilt binaries

Every tagged release publishes self-contained, single-file CLI binaries for `win-x64`,
`linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64` — no .NET runtime required. Download them
from the [Releases](https://github.com/AlexeyDavidenko/MusicOrganizer/releases) page.

### Docker

A multi-architecture image (`linux/amd64`, `linux/arm64`) is published to GitHub Container
Registry on every push to `main`:

```bash
docker run --rm -v /path/to/music:/music ghcr.io/alexeydavidenko/musicorganizer scan /music
```

The image is built on the official `mcr.microsoft.com/dotnet` base images and runs as a non-root
user. To build it locally:

```bash
docker build -t musicorganizer .
```

### From source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/AlexeyDavidenko/MusicOrganizer.git
cd MusicOrganizer
dotnet build
dotnet run --project src/MusicOrganizer.Cli -- --help
```

## Usage

The examples below use `musicorganizer` for the CLI binary. When running from source, substitute
`dotnet run --project src/MusicOrganizer.Cli --`.

```bash
# Inspect the collection
musicorganizer scan /path/to/music
musicorganizer scan /path/to/music --report scan.html --format html

# Recover missing tags: preview first, then apply
musicorganizer recover-tags /path/to/music
musicorganizer recover-tags /path/to/music --apply --report recovered.json --format json

# Repair mis-decoded tag text
musicorganizer fix-encoding /path/to/music --apply

# Rename to Artist-Title.mp3, transliterating Cyrillic to Latin
musicorganizer rename /path/to/music --apply --transliterate

# Move into an Artist/Album tree and remove folders left empty
musicorganizer organize /path/to/music --apply --prune-empty-folders

# Find duplicates and export them for review
musicorganizer find-duplicates /path/to/music --report duplicates.csv --format csv

# Delete exact duplicates
musicorganizer remove-duplicates /path/to/music --apply

# Undo any applied run using the run id it printed
musicorganizer rollback <run-id>

# Prune journal runs older than 7 days
musicorganizer clean-journal --older-than-days 7 --apply
```

Run `musicorganizer <command> --help` for the full list of options.

## Desktop application

```bash
dotnet run --project src/MusicOrganizer.Gui
```

The desktop app exposes all nine commands, each on its own screen with the same options as the
CLI (dry-run / apply, `--transliterate`, `--prune-empty-folders`, and so on). Results stream into
the view as files are processed and can be exported via **Save results…** in any of the four
report formats. The rollback screen lists recent runs from the journal, so there is no need to
copy run ids by hand.

## Architecture

The solution follows Clean Architecture, with dependencies pointing strictly inward:

```
src/
  MusicOrganizer.Domain/           Entities, value objects, domain interfaces (no I/O)
  MusicOrganizer.Application/      Use cases and ports implemented by Infrastructure
  MusicOrganizer.Infrastructure/   TagLibSharp, file system, journal, report writers
  MusicOrganizer.Shared/           Cross-cutting primitives and utilities
  MusicOrganizer.Cli/              Composition root: Generic Host + System.CommandLine
  MusicOrganizer.Gui/              Composition root: Generic Host + Avalonia (MVVM)
tests/
  MusicOrganizer.Tests/            Unit, integration, and architecture tests
```

`Domain` and `Shared` have no dependencies within the solution; `Application` depends only on
them; `Infrastructure` implements the ports that `Application` defines. The CLI and GUI are thin
composition roots that register the same `AddApplicationServices()` and
`AddInfrastructureServices()`, so each feature is implemented exactly once. Layer boundaries are
enforced both by project references and by architecture tests (NetArchTest).

Key design decisions are recorded as Architecture Decision Records in [`docs/adr`](docs/adr).

**Technology stack:** .NET 10, C#,
[Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host),
[System.CommandLine](https://github.com/dotnet/command-line-api),
[Avalonia](https://avaloniaui.net/),
[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet),
[TagLibSharp](https://github.com/mono/taglib-sharp), xUnit, FluentAssertions, NetArchTest.

## Development

Before submitting a change, make sure all quality gates pass:

```bash
dotnet format --verify-no-changes
dotnet build    # TreatWarningsAsErrors and StyleCop.Analyzers are enabled
dotnet test
```

GitHub Actions runs the same checks on Linux, Windows, and macOS for every pull request.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/). Releases are
automated with [Release Please](https://github.com/googleapis/release-please): it keeps a release
pull request open that bumps the version and updates [`CHANGELOG.md`](CHANGELOG.md); merging it
tags the release and publishes the binaries and Docker image.

A [Dev Container](.devcontainer/devcontainer.json) is provided for VS Code and JetBrains Rider.
Reopening the repository in the container provides the .NET SDK, `git`, `gh`, and `docker`, with
the solution restored automatically.

## Roadmap

MP3 is the only supported format today, and all metadata comes from the files themselves. The
architecture is designed to accommodate the following without restructuring existing code:

- Additional audio formats (FLAC, OGG, AAC, …)
- Online metadata sources (MusicBrainz, AcoustID, Discogs, Last.fm)
- Acoustic fingerprinting for duplicate detection

## Project status

Under active development. See the
[latest release](https://github.com/AlexeyDavidenko/MusicOrganizer/releases/latest) and
[`CHANGELOG.md`](CHANGELOG.md); `main` contains all nine commands, the desktop application, and
structured reports, covered by 200+ automated tests.

## License

A license has not been chosen yet. Until one is added, all rights are reserved by the author.
