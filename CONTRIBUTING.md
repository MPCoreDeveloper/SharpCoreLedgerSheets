# Contributing to LedgerSheets

Thanks for taking the time to contribute. This document describes how the project is built, tested
and styled so your change fits in on the first try.

## Getting started

```bash
git clone https://github.com/MPCoreDeveloper/LedgerSheets.git
cd LedgerSheets
dotnet build LedgerSheets.slnx
dotnet test  LedgerSheets.slnx
```

Requirements: the **.NET 11 SDK** (the repository pins it in `global.json`). Nothing else — no
database server, no Node.js.

## Ground rules

1. **Layering.** Dependencies point inwards only:
   `Web -> Reports/Infrastructure -> Application -> Domain`.
   `LedgerSheets.Domain` must stay free of package references and framework types.
   `LaagAfhankelijkhedenTests` fails the build when this is broken.
2. **Zero warnings.** The build runs with the Microsoft analyzers (latest recommended) and
   SonarAnalyzer. A change that introduces a warning is not mergeable — fix the cause rather than
   suppressing the rule.
3. **Test-first.** Every behaviour change comes with a test. Domain rules belong in
   `LedgerSheets.Domain.Tests`, storage behaviour in `LedgerSheets.Infrastructure.Tests`, rendering
   in `LedgerSheets.Reports.Tests`.
4. **Small, focused commits.** One idea per commit, with a message that explains *why*.
5. **Test dependencies stay free.** The test projects use the assertions built into `xunit.v3` —
   no FluentAssertions, Shouldly or similar package. Everything the repository references must be
   free and permissively licensed (MIT / Apache-2.0) so the suite is usable in any context.
   Two conventions that follow from that:
   - The expected value comes first: `Assert.Equal(expected, actual)`, `Assert.Contains(needle, haystack)`.
   - Message checks read `Assert.Contains("...", Assert.Throws<DomeinException>(() => ...).Message`.

## Code style

- C# 15, nullable enabled, file-scoped namespaces, `var` where the type is obvious.
- Private/static helpers are lowercase-camel in expression bodies only when that genuinely reads
  better; prefer explicit bodies with early returns.
- Domain code throws `DomeinException` for rule violations; application code throws
  `NietGevondenException` for missing data. Never throw generic framework exceptions for these.
- XML documentation on public types and members; Dutch domain vocabulary (`Ledger`,
  `JournalEntry`, `BalanceSheet`) is intentional and stays.
- Formats and analyzers are configured in `.editorconfig` and `Directory.Build.props` — match them
  instead of arguing with them.

## Adding a report

1. Create the model in `LedgerSheets.Reports/Models` (inherit `ReportBaseViewModel`).
2. Add a Razor template in `LedgerSheets.Reports/Templates`. Use `ViewData["Labels"]`
   (`IReportLabels`) for **all** user-visible text — never hard-code a label.
3. Register the template in `ReportRegistry.MetStandaardRapporten()` with label keys.
4. Add a rendering test in `LedgerSheets.Reports.Tests` that asserts the document is self-contained.

## Adding a language

1. Copy `src/LedgerSheets.Web/Resources/SharedResource.en.resx` to
   `SharedResource.<culture>.resx` and translate the values (keep the keys).
2. Add the culture to `Languages.Supported`.
3. Run `dotnet test tests/LedgerSheets.ArchitectureTests` — `LocalizationTests` checks that every
   key resolves for every supported culture.

## Pull requests

- Keep the title in the imperative mood and reference the issue it closes.
- Make sure `dotnet build LedgerSheets.slnx` and `dotnet test LedgerSheets.slnx` are green on your
  machine; CI runs both on Linux, Windows and macOS.
- Describe the *behaviour* change and the reasoning. Screenshots help for UI changes.
- By contributing you agree that your contribution is licensed under the [MIT license](LICENSE).

## Reporting bugs

Open an issue with the reproduction steps, the expected and actual behaviour, and the output of
`dotnet --info`. For security issues please do **not** open a public issue; contact the maintainer
directly.

## Design documentation

The reasoning behind the architecture lives in [`docs/`](docs/) as BMAD artifacts
(`01-brief.md`, `02-prd.md`, `03-architecture.md`, `04-epics.md`). Update them when a decision
changes — they are the project's memory.
