# Implementation Plan: SharpCoreLedgerSheets rebrand, English schema, Dutch-first UX

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [`spec.md`](spec.md)

**Input**: Feature specification from `specs/002-rebrand-and-english-schema/spec.md`

## Summary

Rename the product and everything it is called (`LedgerSheets` → `SharpCoreLedgerSheets`), translate the
storage schema from Dutch to English with short names that respect the SharpCoreDB block-name cap, and
turn the implicit Dutch-first behaviour into an explicit, configurable default culture. The engine, the
layering and the packages do not change; only names, the on-disk schema and the default UI culture do.

## Technical Context

**Language/Version**: unchanged - C# 15 on .NET 11 (`net11.0`), SDK pinned in `global.json`

**Primary Dependencies**: unchanged - SharpCoreDB 2.1.2 is still the only runtime package; no package was
added or removed for this change

**Storage**: same engine and file format, new schema: `accounts`, `entries`, `entry_lines`, `banktxns`,
`matchrules` (was `rekeningen`, `posten`, `postregels`, `bankmutaties`, `matchregels`). This is a
breaking change, so the version goes to 2.0.0

**Testing**: xunit.v3 on Microsoft.Testing.Platform; 330 tests, including real encrypted round-trips
that now prove the English schema and a guard that Dutch is the default culture

**Target Platform**: unchanged - Linux, Windows, macOS

**Project Type**: unchanged - one solution, five libraries/host and five test projects, renamed

**Constraints**: derived metadata block names stay under SharpCoreDB's 31-character cap (so
`banktxns`, not `bank_transactions`); zero warnings; no new dependency

**Scale/Scope**: 10 projects and ~210 files renamed mechanically, 6 storage files rewritten by hand

## Constitution Check

*GATE: all gates pass. The constitution was amended to 1.1.0 for this change.*

| Principle | Gate | Result |
| --- | --- | --- |
| I. A pure domain | Domain still has zero package references | PASS - only identifiers changed |
| II. Inward dependencies | Layering untouched | PASS - `LayerDependenciesTests` |
| III. Tests are the evidence | Every renamed behaviour is proven by an existing test; the Dutch default gets a new one | PASS - 330 green |
| IV. Warning-free build | `-warnaserror` clean | PASS - 0 warnings |
| V. Small, offline, encrypted | No new dependency, same single file | PASS |
| VI. Reconcile, never assume | Statements unchanged | PASS |
| VII. Cross-platform parity | Paths and scripts stay portable | PASS |
| VIII. Dutch first, localized everywhere | Dutch default, English neutral fallback, eight cultures | PASS - `Dutch_IsTheDefaultCulture` |
| IX. Permissive licences | No new packages | PASS |

**Amendment**: principle VIII was rewritten from "Dutch is the default and the neutral fallback" to
"Dutch first, localized everywhere" (the neutral file is English), and the constraints table now
prescribes English table and column names.

## Project Structure

```text
SharpCoreLedgerSheets.slnx
src/
├── SharpCoreLedgerSheets.Domain/          # unchanged except identifiers
├── SharpCoreLedgerSheets.Application/     # project renamed; `post` locals renamed to `entry`
├── SharpCoreLedgerSheets.Infrastructure/  # English schema (LedgerSchema, BankStatementSchema) + repositories
├── SharpCoreLedgerSheets.Reports/
└── SharpCoreLedgerSheets.Web/             # Dutch default culture, logo + favicon in wwwroot/img
tests/ (five projects, same layout, renamed)
assets/logo/                               # sharpcoreledgersheets-512.png, -128.png (from Artifacts/)
specs/002-rebrand-and-english-schema/      # this feature
```

## Migration Steps (as executed)

1. Move every project directory and the solution file to the new name.
2. Replace the brand in all tracked text (`LedgerSheets` → `SharpCoreLedgerSheets`, lowercase paths
   too), then rename the `.csproj` files so the assemblies and package IDs follow.
3. Rewrite `LedgerSchema` and `BankStatementSchema` with English DDL, and rename the columns, SQL
   parameters and row keys in the four repositories plus the `ReadOptionalText` helper.
4. Rename the remaining Dutch identifiers in domain/application code (`post` → `entry`,
   `kasMovement` → `cashMovement`).
5. Make Dutch the default culture, add the `Localization:DefaultCulture` override, and guard it with a
   test.
6. Generate PNG brand assets from `Artifacts/` (512/128 px for the README, 192 px logo and 64 px favicon
   for the web app), wire them into `_Layout.cshtml`, `site.css` and the README.
7. Update the specification artifacts, the constitution (1.1.0), `CONTRIBUTING.md` and the upgrade note.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| Abbreviated table names (`banktxns`, `matchrules`) | SharpCoreDB caps derived metadata block names at 31 characters, and lower-case identifiers are the storage contract | `bank_transactions`/`counter_account_rules` would exceed the cap and break the schema at start-up |
| No migration shim for 1.0 files | The product is days old and the schema change is total; a shim would need both schemas alive at once | Keeping two schemas doubles the query surface for data that can simply be re-imported |
