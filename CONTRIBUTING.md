# Contributing to SharpCoreLedgerSheets

Thanks for taking the time to contribute. This document describes how the project is built, tested
and styled so your change fits in on the first try.

## Getting started

```bash
git clone https://github.com/MPCoreDeveloper/SharpCoreLedgerSheets.git
cd SharpCoreLedgerSheets
dotnet build SharpCoreLedgerSheets.slnx
dotnet test  SharpCoreLedgerSheets.slnx
```

Requirements: the **.NET 11 SDK** (the repository pins it in `global.json`). Nothing else — no
database server, no Node.js.

## Ground rules

1. **Layering.** Dependencies point inwards only:
   `Web -> Reports/Infrastructure -> Application -> Domain`.
   `SharpCoreLedgerSheets.Domain` must stay free of package references and framework types.
   `LayerDependenciesTests` fails the build when this is broken.
2. **Zero warnings.** The build runs with the Microsoft analyzers (latest recommended) and
   SonarAnalyzer. A change that introduces a warning is not mergeable — fix the cause rather than
   suppressing the rule.
3. **Test-first.** Every behaviour change comes with a test. Domain rules belong in
   `SharpCoreLedgerSheets.Domain.Tests`, storage behaviour in `SharpCoreLedgerSheets.Infrastructure.Tests`, rendering
   in `SharpCoreLedgerSheets.Reports.Tests`.
4. **Small, focused commits.** One idea per commit, with a message that explains *why*.
5. **Test dependencies stay free.** The test projects use the assertions built into `xunit.v3` —
   no FluentAssertions, Shouldly or similar package. Everything the repository references must be
   free and permissively licensed (MIT / Apache-2.0) so the suite is usable in any context.
   Two conventions that follow from that:
   - The expected value comes first: `Assert.Equal(expected, actual)`, `Assert.Contains(needle, haystack)`.
   - Message checks read `Assert.Contains("...", Assert.Throws<DomainException>(() => ...).Message`.

## Code style

- C# 15, nullable enabled, file-scoped namespaces, `var` where the type is obvious.
- Private/static helpers are lowercase-camel in expression bodies only when that genuinely reads
  better; prefer explicit bodies with early returns.
- Domain code throws `DomainException` for rule violations; application code throws
  `NotFoundException` for missing data. Never throw generic framework exceptions for these.
- XML documentation on public types and members. Type, member, table and column names are English —
  the schema reads `accounts`, `entries`, `entry_lines`, `banktxns` and `matchrules` — so the data model
  is legible to an international contributor. Names stay short because SharpCoreDB caps derived
  metadata block names at 31 characters.
- User-visible text lives in `Resources/SharedResource*.resx`, never in code or templates. Dutch is the
  default culture (the target market is the Dutch MKB) and the neutral resource file is the English
  fallback; a new string means a new key in all eight files.
- A user-facing rule message means a `MessageCode` constant plus a `Msg_<code>` value in **both**
  `SharedResource.resx` (English) and `SharedResource.nl.resx` (Dutch); the Dutch text the exception carries
  is the fallback. `EveryMessageCode_HasATranslationForDutchAndEnglish` fails until both exist.
- Formats and analyzers are configured in `.editorconfig` and `Directory.Build.props` — match them
  instead of arguing with them.

## Adding a report

1. Create the model in `SharpCoreLedgerSheets.Reports/Models` (inherit `ReportBaseViewModel`).
2. Add a Razor template in `SharpCoreLedgerSheets.Reports/Templates`. Use `ViewData["Labels"]`
   (`IReportLabels`) for **all** user-visible text — never hard-code a label.
3. Register the template in `ReportRegistry.WithDefaultReports()` with label keys.
4. Add a rendering test in `SharpCoreLedgerSheets.Reports.Tests` that asserts the document is self-contained.

## Adding a language

1. Copy `src/SharpCoreLedgerSheets.Web/Resources/SharedResource.resx` (the neutral, English file) to
   `SharedResource.<culture>.resx` and translate the values (keep the keys).
2. Add the culture to `Languages.Supported`.
3. Run `dotnet test tests/SharpCoreLedgerSheets.ArchitectureTests` — `LocalizationTests` checks that every
   key resolves for every supported culture.

## Pull requests

- Keep the title in the imperative mood and reference the issue it closes.
- Make sure `dotnet build SharpCoreLedgerSheets.slnx` and `dotnet test SharpCoreLedgerSheets.slnx` are green on your
  machine; CI runs both on Linux, Windows and macOS.
- Describe the *behaviour* change and the reasoning. Screenshots help for UI changes.
- By contributing you agree that your contribution is licensed under the [MIT license](LICENSE).

## Reporting bugs

Open an issue with the reproduction steps, the expected and actual behaviour, and the output of
`dotnet --info`. For security issues please do **not** open a public issue; contact the maintainer
directly.

## Design documentation

The reasoning behind the architecture is recorded with **Spec Kit** — the specification *is* the
contract, and the plan and tasks trace back to it:

| Artifact | Purpose |
| --- | --- |
| [`.specify/memory/constitution.md`](.specify/memory/constitution.md) | The project principles every change is checked against |
| [`specs/001-sharpcoreledgersheets/spec.md`](specs/001-sharpcoreledgersheets/spec.md) | User stories, functional requirements and success criteria |
| [`specs/001-sharpcoreledgersheets/plan.md`](specs/001-sharpcoreledgersheets/plan.md) | Technical design, constitution check and complexity tracking |
| [`specs/001-sharpcoreledgersheets/tasks.md`](specs/001-sharpcoreledgersheets/tasks.md) | Ordered tasks, each naming the artifact that proves it |
| [`specs/001-sharpcoreledgersheets/research.md`](specs/001-sharpcoreledgersheets/research.md) | Decisions, alternatives rejected, risks and lessons learned |

New work starts with a specification in `specs/<feature>/`. With a coding agent that has Spec Kit
installed, the slash commands drive that flow: `/speckit-constitution`, `/speckit-specify`,
`/speckit-plan`, `/speckit-tasks`, `/speckit-implement`, plus `/speckit-clarify`, `/speckit-analyze`
and `/speckit-checklist` for quality. Update the artifacts when a decision changes — they are the
project's memory.
