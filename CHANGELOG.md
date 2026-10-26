# Changelog

## [0.3.0](https://github.com/AlexeyDavidenko/MusicOrganizer/compare/v0.2.0...v0.3.0) (2026-10-26)


### Features

* **recover-tags:** recover year, track number and genre ([46c3b1d](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/46c3b1d0d941e53bf603e8e48c3f5874be1491d4))

## [0.2.0](https://github.com/AlexeyDavidenko/MusicOrganizer/compare/v0.1.0...v0.2.0) (2026-10-16)


### Features

* add --report option to scan/recover-tags/find-duplicates ([574acdb](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/574acdbe17e8e191c74c20b740afe93f9d1c490d))
* add Avalonia desktop GUI with full command parity ([1d2961e](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/1d2961e69376bf830a7bedad0350e3660e6fb82d))
* add clean-journal command and organize --prune-empty-folders ([3613786](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/36137867d46e423a10163cae288122ddbfe392d2))
* add fix-encoding command to detect and correct mis-decoded tag text ([57cb3e0](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/57cb3e0af731152875c72fc7784ac586f3d1605a))
* add organize command to move files into an Artist/Album tree ([e13fb24](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/e13fb243ea165c9eeebe0c4612d9b9b5b887ba68))
* add structured report formats (JSON/CSV/HTML) ([90e12d4](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/90e12d4b277afb88a8c96fc336c097e24d66151c))


### Bug Fixes

* **cli:** accept 'plain' for --format and update --report help text ([285de6f](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/285de6fc4779a525545b4070c61b41d4f051b6d6))
* copy .editorconfig into the Docker build context ([cba1f0c](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/cba1f0c78300e9e4e560210dfdf0edf234244168))
* make FileSystemScanner resilient to unreadable directories ([d144364](https://github.com/AlexeyDavidenko/MusicOrganizer/commit/d1443646488887adf468a48b1c2888b65e9319ef))
