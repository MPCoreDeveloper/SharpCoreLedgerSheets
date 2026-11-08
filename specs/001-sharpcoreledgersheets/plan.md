# Implementation Plan: SharpCoreLedgerSheets 1.0

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [`spec.md`](spec.md)

**Input**: Feature specification from `specs/001-sharpcoreledgersheets/spec.md`

**Note**: Migrated from the technical design that was agreed before the code was written; the design
itself is unchanged and the implementation matches it.

## Summary

A double-entry ledger is the single source of truth; the balance sheet, the profit and loss statement,
the cash flow statement and the key ratios are all derived from that one ledger so they cannot drift
apart. The engine is a set of small .NET 11 / C# 15 libraries (`Domain`, `Application`,
`Infrastructure`, `Reports`) plus an ASP.NET Core Razor Pages host, storing everything in one encrypted
SharpCoreDB file. Reports render from Razor templates to self-contained, print-ready HTML and export
to CSV.

## Technical Context

**Language/Version**: C# 15 on .NET 11 (`net11.0`); SDK pinned in `global.json`
(`11.0.100-rc.3.26522.148`, `rollForward: latestFeature`, prereleases allowed)

**Primary Dependencies**: SharpCoreDB 2.1.2 (embedded encrypted storage, the only runtime package);
`Microsoft.AspNetCore.App` framework reference for the web host and Razor. Build-time and test-only:
SonarAnalyzer.CSharp 10.35.0.4138 (analyzers), xunit.v3 4.0.2 on Microsoft.Testing.Platform,
Microsoft.NET.Test.Sdk 18.11.0, coverlet.collector 10.1.0, NetArchTest.Rules 1.3.2,
Microsoft.Extensions.Localization 11.0.0-rc.3

**Storage**: SharpCoreDB - one encrypted `.scdb` file per administration, master password supplied by
configuration, user secrets or `SharpCoreLedgerSheets__Storage__MasterPassword`

**Testing**: xunit.v3 (built-in assertions only) on Microsoft.Testing.Platform, five test projects
(domain, application, infrastructure, reports, architecture), run with `dotnet test SharpCoreLedgerSheets.slnx`

**Target Platform**: cross-platform .NET libraries (Linux, Windows, macOS) plus an ASP.NET Core web app;
no database server, no Node.js

**Project Type**: layered library suite + a server-rendered web application (single solution,
`SharpCoreLedgerSheets.slnx`)

**Performance Goals**: interactive for a small administration - a dashboard or report request is a
handful of queries over one file, and the bank import page builds every open proposal in one pass, so a
statement of a few hundred movements costs two reads rather than a few hundred round trips

**Constraints**: zero warnings under the Microsoft + Sonar analyzers (`-warnaserror` in CI); no
JavaScript, no CDN and no external URL in generated reports; no headless browser or Node.js anywhere;
single long-lived database handle with serialized writes; amounts rounded to the currency's decimals

**Scale/Scope**: one administration per file; thousands of entries per year; five library/host
projects plus five test projects; about 330 tests

## Constitution Check

*GATE: must pass before Phase 0 research. Re-checked after Phase 1 design - all gates pass.*

| Principle | Gate | Result |
| --- | --- | --- |
| I. A pure domain | `SharpCoreLedgerSheets.Domain` has zero package references and no framework types | PASS - `SharpCoreLedgerSheets.Domain.csproj` has no `PackageReference` |
| II. Inward dependencies | No layer reaches outwards | PASS - enforced by `LayerDependenciesTests` |
| III. Tests are the evidence | Every requirement names a test | PASS - `spec.md` names the artifact per requirement |
| IV. Warning-free build | 0 warnings with `-warnaserror` | PASS - 0 warnings, 0 errors |
| V. Small, offline, encrypted | No Node.js/headless browser/CDN; one encrypted file | PASS - SharpCoreDB is the only runtime package |
| VI. Reconcile, never assume | Balance sheet and cash flow assert their own reconciliation | PASS - `IsBalanced` on both statements |
| VII. Cross-platform parity | Same commands on three operating systems | PASS - CI matrix (ubuntu, windows, macos) |
| VIII. Dutch first, localized everywhere | Dutch default, English neutral fallback, eight cultures | PASS - `LocalizationTests` |
| IX. Permissive licences | Only MIT/Apache-2.0, exceptions documented | PASS WITH JUSTIFICATION - see Complexity Tracking |

## Project Structure

### Documentation (this feature)

```text
specs/001-sharpcoreledgersheets/
├── spec.md              # User stories, requirements, success criteria
├── plan.md              # This file - technical design and constitution check
├── research.md          # Decisions, alternatives rejected, risks, lessons learned
├── tasks.md             # Ordered tasks with the artifact that proves each one
└── checklists/
    └── requirements.md  # Requirements-quality review checklist
```

### Source Code (repository root)

```text
src/
├── SharpCoreLedgerSheets.Domain/            # ledger, statements, ratios, bank movements - no dependencies
│   ├── Accounting/                 # Account, AccountType, Amount, Currency, Period, JournalEntry,
│   │                               #   JournalEntryLine, Ledger, CashFlowCategory
│   ├── Banking/                    # BankStatementFormat, BankTransaction, CounterAccountRule,
│   │                               #   MatchingAssistant, PostingProposal, TextNormalization
│   ├── Common/                     # DomainException
│   ├── Reporting/                  # BalanceSheet, ProfitAndLoss, CashFlowStatement, RatioOverview
│   └── Services/                   # BalanceSheetBuilder, ProfitAndLossBuilder, CashFlowBuilder,
│                                   #   RatioCalculator (+ their interfaces)
├── SharpCoreLedgerSheets.Application/       # use cases + ports
│   ├── Abstractions/               # IAccountRepository, IJournalEntryRepository,
│   │                               #   IBankStatementRepository, ICounterAccountRuleRepository,
│   │                               #   IBankStatementImporter
│   ├── Exceptions/                 # NotFoundException
│   ├── Models/                     # command/result records, FinancialOverview
│   └── Services/                   # AccountService, JournalEntryService, LedgerLoader,
│                                   #   ReportingService, BankStatementService (+ interfaces)
├── SharpCoreLedgerSheets.Infrastructure/    # SharpCoreDB adapters + statement readers
│   ├── Banking/                    # Camt053Importer, CsvImporter, SyntheticReference
│   ├── Extensions/                 # AddSharpCoreLedgerSheetsStorage(...)
│   ├── Options/                    # SharpCoreLedgerSheetsStorageOptions
│   └── Storage/                    # SharpCoreDbFactory, LedgerSchema, BankStatementSchema,
│                                   #   the four repositories
├── SharpCoreLedgerSheets.Reports/           # Razor report engine
│   ├── Abstractions/               # IReportLabels, IReportRenderer, IReportExporter, ...
│   ├── Models/                     # report view models, ReportFormat, ReportMetadata
│   ├── Reporting/                  # EnglishReportLabels, ReportAssets
│   ├── Services/                   # ReportRegistry, HtmlReportRenderer, CsvReportExporter,
│   │                               #   RazorViewToStringRenderer, ReportViewLocationExpander
│   └── Templates/                  # FinancialOverviewReport, BalanceSheetReport,
│                                   #   ProfitAndLossReport, CashFlowReport, Shared/_ReportLayout
└── SharpCoreLedgerSheets.Web/               # ASP.NET Core host
    ├── Localization/               # Languages, LocalizedReportLabels
    ├── Pages/                      # Index (dashboard), Accounts, Journal, Bankimport, Report, Error
    ├── Resources/                  # SharedResource[.nl|.en|.de|.fr|.es|.it|.pt|.pl].resx
    └── Program.cs                  # composition root

tests/
├── SharpCoreLedgerSheets.Domain.Tests/          # Accounting, Banking, Reporting (+ TestLedger scenario)
├── SharpCoreLedgerSheets.Application.Tests/     # Services against in-memory repositories (Fakes/)
├── SharpCoreLedgerSheets.Infrastructure.Tests/  # SharpCoreDB round-trips, CAMT.053/CSV readers
├── SharpCoreLedgerSheets.Reports.Tests/         # Registry, rendering, CSV export
└── SharpCoreLedgerSheets.ArchitectureTests/     # LayerDependenciesTests, LocalizationTests

.specify/                          # Spec Kit: constitution, templates, scripts
```

**Structure Decision**: Clean-architecture layering with one project per layer, because the accounting
rules are the risky part and must be testable without storage, rendering or HTTP. The report engine is
its own project so a host can render reports without the web stack; the web app is the only project
that knows about HTTP and request culture.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| SonarAnalyzer.CSharp is source-available, not OSI-OSS | Its rules catch real defects the Microsoft analyzers miss | Dropping it would weaken a project whose only product is correctness; it is build-time only (`PrivateAssets=all`) and never shipped |
| Five projects instead of one | The domain must stay dependency-free, and reports must render without a web host | A single project would let storage or ASP.NET types leak into the accounting rules, which `LayerDependenciesTests` exists to prevent |
| Razor report engine inside a class library | Templates must render outside an HTTP request | Hand-built HTML strings would lose the model binding, layout and escaping that Razor gives for free |

## Design Detail

### Layering

```text
SharpCoreLedgerSheets.Web                ASP.NET Core host (Razor Pages, localization, DI composition)
  |
  +-- SharpCoreLedgerSheets.Reports      Razor templates, ReportRegistry, IReportRenderer, CSV exporter
  |
  +-- SharpCoreLedgerSheets.Infrastructure   SharpCoreDB adapters + CAMT.053/CSV statement readers
  |
  +-- SharpCoreLedgerSheets.Application      use cases + port definitions (IAccountRepository, ...)
             |
             +-- SharpCoreLedgerSheets.Domain    entities, value objects, statements, ratios (no dependencies)
```

Dependency direction is tested, not merely intended: `LayerDependenciesTests` fails the build if the
domain reaches for ASP.NET, Microsoft.Extensions or SharpCoreDB, or if any layer reaches outwards.

### Why an in-memory ledger?

Statements are computed from a rehydrated `Ledger` rather than from SQL aggregates. The ledger of a
small administration is tiny, and this choice buys three things:

1. the accounting rules live in exactly one place and are unit-testable without a database;
2. the storage engine is replaceable (the ports are `IAccountRepository` / `IJournalEntryRepository`);
3. a report needs history anyway - cumulative equity and the cash *opening* balance require entries
   older than the reporting period.

### Domain model

| Type | Kind | Responsibility |
| --- | --- | --- |
| `Currency` | value object | ISO-4217 code, symbol, decimals |
| `Amount` | value object (struct) | Rounded money; refuses mixed-currency arithmetic |
| `Period` | value object (struct) | Inclusive date range with `Contains` and `ForYear` |
| `Account` | entity | An account and its classification flags |
| `JournalEntry` / `JournalEntryLine` | entity | A balanced entry and its lines |
| `Ledger` | aggregate | Accounts + entries; `AddAccount`, `Post`, `Restore`, `Balance`, `MovementInPeriod` |
| `BalanceSheet`, `ProfitAndLoss`, `CashFlowStatement`, `RatioOverview` | immutable results | The statements and the ratios |
| `BalanceSheetBuilder`, `ProfitAndLossBuilder`, `CashFlowBuilder`, `RatioCalculator` | domain services | Derive the statements from the ledger |
| `BankTransaction` | entity | An imported bank movement: signed amount, the bank's reference, counterparty and status |
| `CounterAccountRule` | entity | A matching rule: keyword → counterparty account, with a confirmation count |
| `PostingProposal` | immutable result | The suggested counterparty account plus its certainty (0-100) and reason |
| `MatchingAssistant` | domain service | `Propose`: IBAN > confirmed keyword > account name/number |

### Sign convention

Every account balance is stored **signed, debit-positive**: `Debit - Credit`. Debit-normal accounts
(assets, costs) are therefore positive when they hold value; credit-normal accounts (liabilities,
equity, revenue) are negative. The presentation layer flips the sign for credit-normal accounts, which
makes the accounting equation fall out of the cancellation of the ledger total:

```text
sum(all balances) = 0
=> Assets = Liabilities + Equity + Result
```

`Ledger.CheckTotal()` returns that sum, and the balance sheet asserts the resulting equation.

### Cash flow derivation

For every entry in the period that touches an account flagged `IsCashEquivalent`, the cash movement is
`sum(Debit - Credit)` over those cash lines, classified by the counterpart accounts:

| Counterpart | Section (`CashFlowCategory`) |
| --- | --- |
| Revenue or costs | Operating |
| Non-cash asset | Investing |
| Liability or equity | Financing |

Movements are aggregated per section and description; entries without a cash movement are skipped. The
result reconciles by construction: opening cash plus the three section totals equals closing cash, and
`CashFlowStatement.IsBalanced` asserts it.

### Application layer

| Service | Responsibility |
| --- | --- |
| `IAccountService` | List, create, rename and deactivate accounts |
| `IJournalEntryService` | List and post entries; resolves account numbers to identities |
| `ILedgerLoader` | Rehydrate a complete `Ledger` from the ports |
| `IReportingService` | Compose the statement builders into a `FinancialOverview` for a period |
| `IBankStatementService` | Import a statement, list movements, propose a counterparty account, book or ignore a movement |

Ports: `IAccountRepository`, `IJournalEntryRepository`, `IBankStatementRepository`,
`ICounterAccountRuleRepository` and `IBankStatementImporter`. The application layer never mentions the
storage engine or a bank format; `AddSharpCoreLedgerSheetsApplication()` registers the use cases and the domain
services.

### Bank import flow

```text
BankStatementService.ImportAsync
    -> IBankStatementImporter (first reader whose CanHandle says yes)
    -> BankTransaction per movement, keyed on bank account + bank reference
    -> IBankStatementRepository.ExistingKeysAsync: duplicates are skipped, not overwritten
BankStatementService.ProposalAsync     -> IMatchingAssistant (one movement)
BankStatementService.ProposalsAsync    -> IMatchingAssistant (a whole list; ledger + learned rules read once)
BankStatementService.PostAsync         -> IJournalEntryService (balanced entry) + rule upsert (confirmations + 1)
```

The import page uses the batch variant: the number of open movements drives the number of proposals,
not the number of times the ledger and the learned rules are read. A statement of a few hundred
movements therefore costs two reads, not a few hundred round trips.

`IBankStatementImporter` is the seam that keeps formats out of the domain: a new bank format is one
class in the infrastructure layer plus one registration. Two rules are deliberate:

1. **Only booked movements.** A CAMT `PDNG` entry can still change or disappear, so booking it would
   create an entry the next statement contradicts.
2. **Import is idempotent.** A movement without a bank reference gets a content-derived `SYN-`
   reference, because identifying a movement by anything less stable would duplicate it on the next
   import of an overlapping period.

### Infrastructure

`AddSharpCoreLedgerSheetsStorage(configure)` wires SharpCoreDB:

- `AddSharpCoreDB()` provides the crypto services and `DatabaseFactory`;
- `ISharpCoreDbFactory.Open()` validates the master password, creates the directory, opens (or creates)
  the `.scdb` file and applies the schema;
- one **singleton** `IDatabase` handle serves the application lifetime and is disposed with the
  container;
- repository calls are serialized on the database handle because the embedded engine is not designed
  for concurrent writers.

Statement readers implement `IBankStatementImporter` and live here, not in the domain:
`Camt053Importer` matches XML elements on their **local name** (so every CAMT.053 version and namespace
is accepted, and `DBIT`/`CRDT` decides the sign), while `CsvImporter` finds columns by their normalised
header name and detects the delimiter and the number notation per file. Both throw a `DomainException`
with a message that can be shown to the user as-is, and both fall back to `SyntheticReference` when the
bank supplies no reference at all.

### Schema

| Table | Purpose | Key columns |
| --- | --- | --- |
| `accounts` | Chart of accounts | `id`, `number`, `name`, `type`, `is_active`, `is_current`, `is_cash_equivalent`, `is_inventory` |
| `entries` | Journal entries | `id`, `date` (ISO-8601 text), `description`, `reference` |
| `entry_lines` | Entry lines | `id`, `entry_id`, `sequence`, `account_id`, `debit`, `credit`, `description` |
| `banktxns` | Imported bank transactions | `id`, `bank_account`, `date`, `amount`, `currency`, `reference`, `counterparty`, `iban`, `description`, `status`, `entry_id` |
| `matchrules` | Matching rules learned from confirmed bookings | `keyword` (unique), `counter_account`, `confirmations` |

Amounts are `DECIMAL`; dates are `yyyy-MM-dd` text so they sort chronologically. **Every table and
index name is deliberately short**: SharpCoreDB derives metadata block names such as
`table:<table>:overflow` and caps a block name at 31 characters.

Rehydration uses `Ledger.Restore` rather than `Ledger.Post`: historical entries may reference accounts
that have since been deactivated, so the "account must be active" rule applies when posting, not when
loading. Balance and account existence are still enforced on load.

### Reports

```text
ReportRegistry  ->  IReportRenderer (HtmlReportRenderer -> Razor templates)
                ->  IReportExporter (CsvReportExporter)
```

`RazorViewToStringRenderer` renders a compiled `.cshtml` outside an HTTP request using a synthetic
`ActionContext`; `ReportViewLocationExpander` teaches the view engine about `/Templates/`. Everything a
template needs - styling, labels, header data - arrives through `ViewData`, so the layout never touches
`Model` and templates never hard-code a word. `IReportLabels` is the seam that keeps the engine
language-neutral: the web app binds it to `IStringLocalizer`, `EnglishReportLabels` is the neutral
default, and a test can bind it to anything.

`AddSharpCoreLedgerSheetsReports()` also registers `DiagnosticSource`/`DiagnosticListener` with `TryAdd`, so the
engine renders inside a bare service provider (tests, console tools) as well as inside a full web host.

Reports are HTML + CSV. PDF comes from the browser's print dialog on the already print-ready HTML, which
keeps a headless browser out of the package; register a custom `IReportRenderer` when server-side PDF is
genuinely required.

### Web layer

Razor Pages, server-rendered, with `AddLocalization()` and request localization configured for the eight
supported cultures. `QueryStringRequestCultureProvider` handles `?culture=de`, a cookie provider
remembers the choice, and a short middleware writes that cookie with `HttpOnly`, `Secure` and
`SameSite=Lax`. `FinPageModel` converts expected domain/storage failures into readable messages instead
of stack traces.

Pages stay thin: `Accounts` and `Journal` pass what they are given to a service, and `Bankimport`
uploads a statement, shows the proposal per open movement and offers *book* or *ignore* - the assistant
decides nothing on its own, the user always confirms.

The `SharedResource` marker type lives in `Resources/` next to its `.resx` files, which is exactly what
the default `ResourceManagerStringLocalizer` naming convention expects - so
`IStringLocalizer<SharedResource>` resolves `SharpCoreLedgerSheets.Web.Resources.SharedResource[.<culture>]`.

### Testing strategy

| Layer | Approach |
| --- | --- |
| Domain | Pure unit tests over a shared ledger scenario (`TestLedger`) with hand-computed expectations; bank movements, matching rules and the assistant are covered the same way |
| Application | Use cases against in-memory repositories that implement the ports |
| Infrastructure | Real encrypted SharpCoreDB databases in the temp folder, including reopen durability, plus the CAMT.053/CSV readers against real statement fragments |
| Reports | Full Razor rendering through a real service provider, plus CSV assertions |
| Cross-cutting | NetArchTest layer rules, naming rules and localization completeness |

Three rules keep the suites honest:

1. **Hand-computed expectations.** The domain scenarios assert concrete totals (18 300 / 6 000 / 10 000 /
   2 300) rather than recomputing them with the code under test.
2. **Round-trip, then reopen.** Storage tests always close and reopen the database before asserting,
   because persistence bugs hide behind a warm cache.
3. **Render, don't mock.** Report tests run the real Razor pipeline; mocking the view engine would prove
   nothing about the templates.

Test infrastructure stays in the test assemblies; production code carries no test hooks.
