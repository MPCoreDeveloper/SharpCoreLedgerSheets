# Tasks: SharpCoreLedgerSheets rebrand, English schema, Dutch-first UX

**Input**: `specs/002-rebrand-and-english-schema/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`

**Organization**: One phase per concern, in the order the work was executed. Every task is complete
(`[x]`) and names the artifact that proves it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies)
- **[Story]**: US1 (Dutch default), US2 (English schema), US3 (language switching), US4 (branding)

---

## Phase 1: Brand rename

- [x] T001 [P] [US4] Move the ten project directories and the solution to `SharpCoreLedgerSheets.*` (`src/`, `tests/`, `SharpCoreLedgerSheets.slnx`)
- [x] T002 [P] [US4] Replace the brand in every tracked text file — namespaces, `using`s, project references, XAML-free markup, resources, docs, CI workflow (177 files)
- [x] T003 [US4] Rename the `.csproj` files so assembly names, package IDs and `RootNamespace` follow (10 projects)
- [x] T004 [P] [US4] Rebrand the packaging metadata in `Directory.Build.props` (`Product`, `RepositoryUrl`, `PackageProjectUrl`, version `2.0.0`)
- [x] T005 [P] [US4] Rename the configuration section and the database file (`SharpCoreLedgerSheets:Storage`, `App_Data/sharpcoreledgersheets.scdb`) in `appsettings.json`, `Program.cs` and the README
- [x] T006 [US4] Generate the brand assets from `Artifacts/*.jpg`: `assets/logo/sharpcoreledgersheets-{512,128}.png`, `wwwroot/img/logo.png` (192 px) and `wwwroot/img/favicon.png` (64 px)
- [x] T007 [US4] Show the brand in the product: logo + localized name in `Pages/Shared/_Layout.cshtml`, favicon link, `.brand-logo` in `site.css`, logo header in `README.md`

## Phase 2: English storage schema

- [x] T008 [US2] Rewrite `LedgerSchema` with English DDL: `accounts`, `entries`, `entry_lines` and English columns/indexes, with the block-name rationale in the remarks
- [x] T009 [P] [US2] Rewrite `BankStatementSchema` with English DDL: `banktxns`, `matchrules`
- [x] T010 [US2] Rename columns, SQL parameters and row keys in `SharpCoreDbAccountRepository`, `SharpCoreDbJournalEntryRepository`, `SharpCoreDbBankStatementRepository` and `SharpCoreDbCounterAccountRuleRepository`
- [x] T011 [P] [US2] Rename the Dutch helper `ReadOptioneleText` → `ReadOptionalText` in `SharpCoreDbRepository`
- [x] T012 [P] [US2] Rename Dutch leftovers outside storage: `post` local/parameter names in the domain and application layers (and the test fakes), `kasMovement` → `cashMovement`
- [x] T013 [US2] Prove the schema with the existing infrastructure tests: real encrypted databases, round-trips and close/reopen durability now run against the English names (79 tests)

## Phase 3: Dutch-first localization

- [x] T014 [US1] Make Dutch the default: `Languages.Default = "nl"`, `Supported` starts with `nl`, documented in the type remarks
- [x] T015 [US1] Read `SharpCoreLedgerSheets:Localization:DefaultCulture` in `Program.cs`, validated against `Languages.IsSupported`, defaulting to Dutch; ship it as `nl` in `appsettings.json`
- [x] T016 [P] [US1] Guard the decision with `LocalizationTests.Dutch_IsTheDefaultCulture` (default is `nl` and it is first in the picker)
- [x] T017 [P] [US3] Keep the switching chain intact: query string → cookie → browser header; English remains the neutral fallback and `de/fr/es/it/pt/pl` stay complete
- [x] T018 [US3] Update the resx brand value (`AppTitle`) in all eight files and let the footer use the localized title

## Phase 4: Documentation and verification

- [x] T019 [P] Rewrite the README sections that changed: logo header, Dutch-first callout, English storage-schema table, localization section with the culture override, upgrade note, requirements
- [x] T020 [P] Update `CONTRIBUTING.md`: English naming rule for types *and* schema, user-visible text in resx, Dutch default culture
- [x] T021 [P] Amend the constitution to 1.1.0: principle VIII becomes "Dutch first, localized everywhere", constraints list the English schema and the 31-character cap
- [x] T022 [P] Update `specs/001-sharpcoreledgersheets/` where facts moved: schema table in `plan.md`, version and test count in `spec.md` and `tasks.md`
- [x] T023 Add this feature record (`specs/002-rebrand-and-english-schema/`): spec, plan, tasks, research
- [x] T024 Verify: `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` → 0 warnings, 0 errors; `dotnet test` → 330 passed; live smoke test on `/` (Dutch), `/?culture=en` (English), `/img/logo.png` (200) and the favicon

---

## Verification Log

| Gate | Result |
| --- | --- |
| `dotnet build SharpCoreLedgerSheets.slnx -c Release -warnaserror` | Succeeds - **0 warnings / 0 errors** |
| `dotnet test SharpCoreLedgerSheets.slnx -c Release` | **330 passed**, 0 failed, 0 skipped (146 domain, 56 application, 79 infrastructure, 26 reports, 23 architecture) |
| Live smoke test | `GET /` → 200 with `Balans` and `lang="nl"`; `GET /?culture=en` → 200 with "Balance sheet"; `/img/logo.png` → 200 (54 208 bytes); favicon link rendered |
| Leftovers | No Dutch identifier remains in `Infrastructure/Storage`; no `LedgerSheets` reference remains outside the historical upgrade note and this feature record |

## Dependencies & Execution Order

Phases run in the order above: the rename unblocks everything, the schema rewrite comes before the tests
that prove it, the localization change is independent of both, and documentation closes the loop.
Phase 2 and Phase 3 could run in parallel - they touch different files - but the verification runs once,
at the end, against the combined result.

## Notes

- `[P]` marks tasks that touch different files and could have run in parallel.
- The 31-character block-name cap is the reason two tables are abbreviated; the rationale lives in the
  DDL, not only in this file.
- The first post-2.0 item is moving the Dutch rule messages out of the code and into resources, so the
  interface is fully localizable (see `research.md`).
