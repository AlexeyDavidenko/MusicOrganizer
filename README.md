# MusicOrganizer

[![CI](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml/badge.svg)](https://github.com/AlexeyDavidenko/MusicOrganizer/actions/workflows/ci.yml)

Кроссплатформенное CLI-приложение на .NET 10 для профессионального управления большой музыкальной
коллекцией MP3 (рассчитано на коллекции от сотен тысяч до миллиона+ файлов).

## Возможности

Реализовано:

- Сканирование коллекции и чтение ID3-тегов (`scan`)
- Восстановление отсутствующих Artist/Title из имени файла и структуры папок (`recover-tags`)
- Переименование файлов по шаблону `Artist-Title.mp3` (`rename`), опционально с транслитерацией
  кириллицы в латиницу (`--transliterate`, BGN/PCGN)
- Поиск дубликатов — точное совпадение по содержимому и вероятностное по тегам (`find-duplicates`)
- Удаление точных дубликатов с сохранением файла с самым коротким путём (`remove-duplicates`)
- Dry Run, Transaction Log и откат (`rollback`) для всех мутирующих операций

Восстановление тегов покрывает все 8 источников из спецификации: ID3, имя файла (в т.ч.
расширенные эвристики — номер трека, шумовые суффиксы), структура папок, консенсус по соседним
файлам в папке, и отчёт о файлах, которые не удалось разобрать ни одним источником.

В планах (см. `docs/TODO.md` — локальный, не в этом репозитории):

- Определение и исправление некорректных кодировок
- Организация структуры коллекции (Artist/Album/Track), отчёты

## Технологии

.NET 10 · C# · [Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host) ·
[System.CommandLine](https://github.com/dotnet/command-line-api) ·
[TagLibSharp](https://github.com/mono/taglib-sharp) · xUnit · FluentAssertions

## Архитектура

Clean Architecture, шесть проектов:

```
src/
  MusicOrganizer.Domain/           — сущности, value objects, доменные интерфейсы
  MusicOrganizer.Application/      — use cases, порты для Infrastructure
  MusicOrganizer.Infrastructure/   — TagLibSharp, файловая система, journal/rollback
  MusicOrganizer.Shared/           — сквозные примитивы/утилиты
  MusicOrganizer.Cli/              — composition root: Generic Host + System.CommandLine
tests/
  MusicOrganizer.Tests/            — Unit/ и Integration/
```

Domain и Shared ни от чего не зависят; зависимости между слоями идут строго в одну сторону
(Application → Domain/Shared, Infrastructure → Application/Domain/Shared, Cli → всё остальное).

## Сборка и запуск

Требуется [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/MusicOrganizer.Cli -- --help
```

### Примеры

```bash
# Сканирование: рекурсивно находит *.mp3, читает теги, печатает отчёт
dotnet run --project src/MusicOrganizer.Cli -- scan /path/to/music

# Восстановление тегов: dry-run по умолчанию, --apply — реальная запись
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- recover-tags /path/to/music --apply

# Переименование по шаблону Artist-Title.mp3
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply

# То же самое, но с транслитерацией кириллицы в латиницу (BGN/PCGN)
dotnet run --project src/MusicOrganizer.Cli -- rename /path/to/music --apply --transliterate

# Откат последней применённой операции (recover-tags/rename с --apply печатают run id)
dotnet run --project src/MusicOrganizer.Cli -- rollback <run-id>

# Поиск дубликатов (read-only)
dotnet run --project src/MusicOrganizer.Cli -- find-duplicates /path/to/music

# Удаление точных дубликатов: dry-run по умолчанию, --apply — реальное удаление
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music
dotnet run --project src/MusicOrganizer.Cli -- remove-duplicates /path/to/music --apply
```

Ни одна ошибка на отдельном файле не прерывает обработку остальной коллекции.

## Docker

```bash
docker build -t musicorganizer .
docker run --rm -v /path/to/music:/music musicorganizer scan /music
```

Multi-stage образ на официальных `mcr.microsoft.com/dotnet` образах, non-root пользователь,
поддержка `linux/amd64` и `linux/arm64`. Образы для `main` публикуются в GHCR
(`ghcr.io/alexeydavidenko/musicorganizer`) при каждом пуше.

## Разработка

Перед коммитом: `dotnet format --verify-no-changes`, `dotnet build` (0 warnings, включён
`TreatWarningsAsErrors`), `dotnet test`. CI (GitHub Actions) прогоняет то же самое на
Linux/Windows/macOS при каждом PR.

## Статус проекта

Активная разработка: 5 команд реализовано (`scan`, `recover-tags`, `rename`, `find-duplicates`,
`remove-duplicates`) плюс общий Journal/Rollback, 99 тестов, CI/CD пайплайн с автосборкой
Docker-образа и релизами по тегам.

## Лицензия

Пока не определена.
