# Tasks: SharpCoreLedgerSheets 1.0

**Input**: Design documents from `specs/001-sharpcoreledgersheets/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`

**Tests**: This project is test-first by constitution (principle III), so every behaviour task is paired
with the test that proves it. The task is done when its test fails without it and passes with it.

**Organization**: Tasks are grouped by phase and by user story. All tasks below are complete (`[x]`) -
this file is the evidence trail of the delivered v1.0, kept current so a later change can see what
already exists and which test guards it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies)
- **[Story]**: which user story the task belongs to (US1..US4)
- Every task names the artifact that proves it

## Path Conventions

Single solution at the repository root: sources in `src/`, tests in `tests/`, one project per layer.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: a solution that builds, warns about nothing and can be packed.

- [x] T001 Create `SharpCoreLedgerSheets.slnx` with `src/SharpCoreLedgerSheets.{Domain,Application,Infrastructure,Reports,Web}` and `tests/SharpCoreLedgerSheets.{Domain,Application,Infrastructure,Reports,Architecture}Tests`
- [x] T002 [P] Pin the SDK in `global.json` (`rollForward: latestFeature`, `test.runner: Microsoft.Testing.Platform`)
- [x] T003 [P] Share build settings in `Directory.Build.props`: `LangVersion 15.0`, nullable, implicit usings, `net11.0` per project
- [x] T004 [P] Configure `.editorconfig` (style, analyzers) and `.gitignore` (build output, `.scdb`, secrets, `.refactor/`)
- [x] T005 [P] Enable the analyzers: `EnableNETAnalyzers`, `AnalysisLevel latest`, `AnalysisMode Recommended`, `SonarAnalyzer.CSharp` with `PrivateAssets=all`
- [x] T006 [P] Wire the xunit.v3 + Microsoft.Testing.Platform harness in every test project (`UseMicrosoftTestingPlatformRunner`, `Using Include="Xunit"`)
- [x] T007 [P] Add packaging metadata and the MIT licence: `LICENSE`, `Directory.Build.props` (`PackageLicenseExpression`, symbols, `snupkg`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the domain kernel every story depends on. No story work starts before this is green.

**⚠️ CRITICAL**: the whole product rests on these value objects and the ledger invariants.

- [x] T008 [P] Money value objects in `src/SharpCoreLedgerSheets.Domain/Accounting/{Currency,Amount}.cs` + `tests/SharpCoreLedgerSheets.Domain.Tests/Accounting/{CurrencyTests,AmountTests}.cs` (rounding, mixed-currency throws)
- [x] T009 [P] `Period` value object in `src/SharpCoreLedgerSheets.Domain/Accounting/Period.cs` + `PeriodTests.cs` (`Contains`, `ForYear`, inclusive bounds)
- [x] T010 [P] `DomainException` in `src/SharpCoreLedgerSheets.Domain/Common/DomainException.cs` - every rule violation is stated in a message a user can read
- [x] T011 `Account` + `AccountType` in `src/SharpCoreLedgerSheets.Domain/Accounting/{Account,AccountType}.cs` + `AccountTests.cs` (five types, flag validation, rename/deactivate)
- [x] T012 `JournalEntry` / `JournalEntryLine` in `src/SharpCoreLedgerSheets.Domain/Accounting/{JournalEntry,JournalEntryLine}.cs` + `JournalEntryTests.cs` (debit xor credit, at least two lines, totals must match)
- [x] T013 `Ledger` aggregate in `src/SharpCoreLedgerSheets.Domain/Accounting/Ledger.cs` + `LedgerTests.cs` (`AddAccount`, `Post` validates existence/active/balance/duplicate, `Restore` skips the active check, `Balance`, `MovementInPeriod`, `CheckTotal`)
- [x] T014 [P] Shared scenario fixture `tests/SharpCoreLedgerSheets.Domain.Tests/TestLedger.cs` with hand-computed totals (18 300 / 6 000 / 10 000 / 2 300)

**Checkpoint**: the domain builds and its invariants are proven without a database.

---

## Phase 3: User Story 1 - Post the books and read the three statements (P1) 🎯 MVP

**Goal**: one ledger in, three reconciling statements out - persisted in one encrypted file and visible
in a dashboard.

**Independent Test**: post entries through `IJournalEntryService` and assert
`ReportingService.BuildOverviewAsync` returns a balanced balance sheet and a reconciling cash flow
statement, before and after reopening the database.

- [x] T015 [P] [US1] `BalanceSheet` + `BalanceSheetBuilder` in `src/SharpCoreLedgerSheets.Domain/Reporting/` and `Services/` + `BalanceSheetBuilderTests.cs` (credit-normal presentation, empty accounts omitted, `IsBalanced`)
- [x] T016 [P] [US1] `ProfitAndLoss` + `ProfitAndLossBuilder` + `ProfitAndLossBuilderTests.cs` (period scope, revenue - costs = result)
- [x] T017 [P] [US1] `CashFlowStatement` (as `CashFlowOverview`) + `CashFlowBuilder` + `CashFlowBuilderTests.cs` (operating/investing/financing derived from the counterpart, opening + movement = closing, non-cash entries excluded)
- [x] T018 [P] [US1] `RatioOverview` + `RatioCalculator` + `RatioCalculatorTests.cs` (five ratios, zero denominator reports *not available*)
- [x] T019 [US1] Ports in `src/SharpCoreLedgerSheets.Application/Abstractions/` (`IAccountRepository`, `IJournalEntryRepository`) and use cases `AccountService`, `JournalEntryService`, `LedgerLoader` + `AccountServiceTests`, `JournalEntryServiceTests`, `LedgerLoaderTests`
- [x] T020 [US1] `ReportingService` composing the builders into `FinancialOverview` + `ReportingServiceTests.cs` over `TestAdministration`
- [x] T021 [US1] SharpCoreDB storage: `SharpCoreDbFactory`, `LedgerSchema`, `SharpCoreDbAccountRepository`, `SharpCoreDbJournalEntryRepository` + `SharpCoreDbAccountRepositoryTests`, `SharpCoreDbJournalEntryRepositoryTests`, including close-and-reopen durability
- [x] T022 [US1] Composition root `src/SharpCoreLedgerSheets.Web/Program.cs` + dashboard `Pages/Index.cshtml(.cs)`; smoke test returns 200 with localized content

---

## Phase 4: User Story 2 - Print and export the statements (P2)

**Goal**: every statement leaves the tool as self-contained HTML or RFC 4180 CSV.

**Independent Test**: render all four reports through the real Razor pipeline and assert the HTML is
complete, styled, JavaScript-free and URL-free; export each to CSV and assert the content.

- [x] T023 [P] [US2] `ReportRegistry` with `WithDefaultReports()` and `ReportKeys` in `src/SharpCoreLedgerSheets.Reports/Services/` + `ReportRegistryTests.cs` (four reports, unique keys, every template name registered)
- [x] T024 [P] [US2] `RazorViewToStringRenderer` + `ReportViewLocationExpander` so compiled `.cshtml` renders outside an HTTP request
- [x] T025 [P] [US2] `HtmlReportRenderer` + `Abstractions/ReportRenderingException` + `HtmlReportRendererTests.cs` (complete document, embedded styling, no `<script>`, no external URL, unknown template explains itself)
- [x] T026 [P] [US2] Four templates in `src/SharpCoreLedgerSheets.Reports/Templates/` plus `Shared/_ReportLayout.cshtml`; all text from `IReportLabels`, styling embedded
- [x] T027 [P] [US2] `CsvReportExporter` + `CsvReportExporterTests.cs` (RFC 4180, invariant numbers, quoting when needed)
- [x] T028 [US2] Report viewer `Pages/Report.cshtml(.cs)` with CSV download and print; smoke test on the HTML handler

---

## Phase 5: User Story 3 - Import a bank statement and book it with the assistant (P3)

**Goal**: a CAMT.053 or CSV statement lands in the ledger without retyping, with a proposal per movement
that the user confirms.

**Independent Test**: import a statement fragment twice and assert only booked movements were read, the
second import added nothing, and re-importing an overlapping period stays idempotent.

- [x] T029 [P] [US3] `BankStatementFormat`, `BankTransaction`, `BankTransactionStatus` in `src/SharpCoreLedgerSheets.Domain/Banking/` + `BankTransactionTests.cs` (signed amount, bank account + reference = import key)
- [x] T030 [P] [US3] `CounterAccountRule` + `CounterAccountRuleTests.cs` (keyword → counterparty account, confirmation count, one rule per keyword)
- [x] T031 [P] [US3] `PostingProposal` + `MatchingAssistant` + `TextNormalization` + `MatchingAssistantTests`, `PostingProposalTests` (IBAN 100 > confirmed keyword 90 > account name/number 70, reason always stated)
- [x] T032 [P] [US3] `Camt053Importer` in `src/SharpCoreLedgerSheets.Infrastructure/Banking/` + `Camt053ImporterTests.cs` (local-name matching, `DBIT`/`CRDT` sign, booked only, reference fallback chain)
- [x] T033 [P] [US3] `CsvImporter` + `SyntheticReference` + `CsvImporterTests.cs` (header-name columns, delimiter and number notation detected, quoted fields, stable `SYN-` fallback)
- [x] T034 [US3] `BankStatementService` + ports (`IBankStatementImporter`, `IBankStatementRepository`, `ICounterAccountRuleRepository`) + `BankStatementServiceTests.cs` (import counts, skip duplicates, batch proposals reading ledger and rules once, post = balanced entry + rule upsert, booked movement cannot be ignored)
- [x] T035 [US3] `SharpCoreDbBankStatementRepository` + `SharpCoreDbCounterAccountRuleRepository` + their tests, including close-and-reopen durability
- [x] T036 [US3] Bank import page `Pages/Bankimport.cshtml(.cs)` with `BankImportLine` (upload, proposal per movement, book or ignore)

---

## Phase 6: User Story 4 - Embed the engine in another application (P4)

**Goal**: a developer can use the engine without the web host.

**Independent Test**: register the services in a bare `ServiceCollection`, post an entry, read the
overview, reopen, assert - no `WebApplication` involved.

- [x] T037 [US4] `AddSharpCoreLedgerSheetsApplication()` / `AddSharpCoreLedgerSheetsStorage()` / `AddSharpCoreLedgerSheetsReports()` in the `Extensions` folders, each usable on its own
- [x] T038 [US4] Package the libraries (`IsPackable`, `IsPackable=false` for the web host, symbols, `snupkg`, tags and description in `Directory.Build.props`)
- [x] T039 [US4] Document the library path in `README.md` ("Using the engine as a library") and verify the snippet compiles against the shipped API

---

## Phase 7: Localization (cross-cutting, serves every story)

- [x] T040 [P] Eight resource files in `src/SharpCoreLedgerSheets.Web/Resources/` (`SharedResource.resx` as neutral/English, `.nl`, `.de`, `.fr`, `.es`, `.it`, `.pt`, `.pl`) + `Languages.Supported` in `src/SharpCoreLedgerSheets.Web/Localization/Languages.cs`
- [x] T041 [P] Request localization in `Program.cs`: query string `?culture=xx`, cookie persistence, browser-header fallback, `HttpOnly`/`Secure`/`SameSite=Lax`
- [x] T042 [P] `LocalizedReportLabels` binding `IReportLabels` to `IStringLocalizer`, with `EnglishReportLabels` as the neutral default outside the web host
- [x] T043 Completeness guard `tests/SharpCoreLedgerSheets.ArchitectureTests/LocalizationTests.cs` (`EveryLanguage_ContainsAllKeys` for all eight cultures, fallback behaviour)

---

## Phase 8: OSS Delivery & Polish

**Purpose**: someone else can clone, build, trust and contribute.

- [x] T044 [P] Architecture guard `tests/SharpCoreLedgerSheets.ArchitectureTests/LayerDependenciesTests.cs`: inward dependencies only, domain free of packages, naming rules
- [x] T045 [P] CI in `.github/workflows/ci.yml`: restore → build `-warnaserror` → test → pack `artifacts/` on ubuntu/windows/macos, packages uploaded from Linux
- [x] T046 [P] Dependency audit job in `.github/workflows/ci.yml`: `dotnet list SharpCoreLedgerSheets.slnx package --vulnerable --include-transitive`
- [x] T047 [P] `README.md` (why, highlights, architecture, quick start, library sample, reports, bank import, localization, testing, requirements) and `CONTRIBUTING.md` (ground rules, style, adding a report, adding a language)
- [x] T048 Adopt Spec Kit: `.specify/` (constitution, templates, scripts, cline integration) and `specs/001-sharpcoreledgersheets/` (`spec.md`, `plan.md`, `tasks.md`, `research.md`, `checklists/requirements.md`); retired the earlier BMAD artifacts in `docs/`
- [x] T049 Verify: `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` → 0 warnings, 0 errors; `dotnet test SharpCoreLedgerSheets.slnx` → 330 passed; live smoke test on dashboard, `?culture=en` and the HTML report handler

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup and BLOCKS every user story - the value objects and ledger
  invariants are the vocabulary the rest of the code speaks.
- **US1 (Phase 3)**: after Foundational. Delivers the MVP: a ledger, the three statements and a dashboard.
- **US2 (Phase 4)**: after US1 - the reports render statements, so they need statements to render.
- **US3 (Phase 5)**: after US1 - an import books an entry and asks the ledger for counterparty accounts.
- **US4 (Phase 6)**: can follow US1/US2 - the registration extensions package what already exists.
- **Localization (Phase 7)**: runs alongside US2 through US4; the completeness test guards all of it.
- **Delivery (Phase 8)**: last, then verified end to end.

### Within Each Story

The test comes first and must fail; models come before services, services before pages, and the story is
demonstrable on its own before the next priority starts.

### Parallel Opportunities

- All Setup tasks marked `[P]` (T002-T007).
- The four statement builders (T015-T018) touch separate files and run in parallel.
- The report pieces (T023-T027) touch separate files and run in parallel.
- The bank-import domain pieces (T029-T031) and the two readers (T032-T033) run in parallel.
- The resource files, the request-localization wiring and the report labels (T040-T042) run in parallel.

---

## Verification Log

| Gate | Result |
| --- | --- |
| `dotnet build SharpCoreLedgerSheets.slnx -c Release -warnaserror` | Succeeds - **0 warnings / 0 errors** |
| `dotnet test SharpCoreLedgerSheets.slnx -c Release` | **330 passed**, 0 failed, 0 skipped (146 domain, 56 application, 79 infrastructure, 26 reports, 23 architecture) |
| Live smoke test | Dashboard `200`; `?culture=en` renders "Balance sheet"; the HTML report handler returns a self-contained document with embedded CSS |

## Post-1.0 Backlog

Ordered by value, not committed - each item starts as a new `specs/<feature>/spec.md`:

1. **VAT handling** - rates per line, a VAT return summary, and VAT payable/receivable accounts.
2. **Multiple currencies** - per-entry rate, revaluation, and a currency-aware cash flow statement.
3. **Invoice generation** - from posted entries, with PDF through a pluggable renderer.
4. **Ledger CSV/Excel round-trip** - import and export for migration and archival.
5. **Multi-administration** - one file per client, selectable, with consolidation later.
6. **Audit trail** - immutable entry history with correcting entries instead of edits.

## Notes

- `[P]` marks tasks that touch different files and can run in parallel.
- Every task names the artifact that proves it; a task without a test or a smoke result is not done.
- Commit after each task or logical group, and keep the message focused on *why*.
- Stop at a checkpoint to validate a story on its own before moving to the next priority.
