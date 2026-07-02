# MusicOrganizer

Кроссплатформенное CLI-приложение на .NET 10 для профессионального управления большой музыкальной
коллекцией MP3 (рассчитано на коллекции от сотен тысяч до миллиона+ файлов).

> Проект в ранней стадии разработки (bootstrap). Часть функциональности, описанной ниже, ещё не
> реализована — актуальный статус см. в разделе [Статус проекта](#статус-проекта).

## Возможности (целевые)

- Восстановление повреждённых и отсутствующих ID3-тегов (ID3v1, ID3v2.2–2.4)
- Определение и исправление некорректных кодировок в тегах
- Переименование файлов и папок по настраиваемым шаблонам
- Организация структуры коллекции (Artist/Album/Track)
- Поиск дубликатов
- Построение отчётов
- Dry Run, Transaction Log и безопасный откат (rollback) для любой операции

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

### Пример: сканирование коллекции

```bash
dotnet run --project src/MusicOrganizer.Cli -- scan /path/to/music
```

Рекурсивно находит все `*.mp3` под указанной папкой, читает теги и печатает построчный отчёт
(`[OK]`/`[ERROR]`) плюс итоговую сводку. Ни одна ошибка на отдельном файле не прерывает скан
остальной коллекции.

## Разработка

Перед коммитом: `dotnet format --verify-no-changes`, `dotnet build` (0 warnings, включён
`TreatWarningsAsErrors`), `dotnet test`.

## Статус проекта

Ранняя стадия: скаффолдинг Clean Architecture завершён, реализована первая вертикальная фича
(сканирование папки → чтение тегов → отчёт).

## Лицензия

Пока не определена.
