# 04 — Epics and stories

> BMAD phase: **Build → Verify**. Every story names the artifact that proves it.

## Epic 1 — Ledger core (domain)

*Goal: a double-entry ledger that makes invalid bookkeeping impossible.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 1.1 | Money, currency and period value objects | Amounts round to the currency's decimals; mixed-currency arithmetic throws | `BedragTests`, `ValutaTests`, `PeriodeTests` |
| 1.2 | Account entity with classification flags | Five types; current/inventory/cash flags validated per type | `RekeningTests` |
| 1.3 | Journal entry with balanced lines | Debit XOR credit; at least two lines; totals must match | `JournaalpostTests` |
| 1.4 | Ledger aggregate | Unique account numbers; posting validates existence, active state and balance | `GrootboekTests` |
| 1.5 | Rehydration | A stored ledger loads back with the same identities and balances | `GrootboekLaderTests`, infrastructure reopen tests |

**Status: done.**

## Epic 2 — The three statements and the ratios

*Goal: derive every statement from the one ledger, and prove they reconcile.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 2.1 | Balance sheet | Assets, liabilities, equity and result as of a date; credit-normal accounts presented positive; balance asserted | `BalansOpstellerTests` |
| 2.2 | Profit and loss statement | Revenue, expenses and result for a period | `WinstVerliesOpstellerTests` |
| 2.3 | Cash flow statement | Operating/investing/financing sections, opening and closing cash, reconciliation asserted | `KasstroomOpstellerTests` |
| 2.4 | Ratios | Current, quick, solvency, net margin, return on equity; unavailable instead of a division error | `RatioBerekeningTests` |

**Status: done.**

## Epic 3 — Use cases

*Goal: an API a host can drive without knowing the ledger internals.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 3.1 | Storage ports | Accounts and entries are abstractions the application owns | `IRekeningRepository`, `IJournaalpostRepository` |
| 3.2 | Account service | Create, rename, deactivate; duplicate numbers rejected | `RekeningServiceTests` |
| 3.3 | Entry service | Account numbers resolved; inactive/unknown accounts and unbalanced entries rejected | `JournaalpostServiceTests` |
| 3.4 | Reporting service | One call returns balance sheet, P&L, cash flow and ratios for a period | `RapportageServiceTests` |

**Status: done.**

## Epic 4 — Encrypted embedded storage

*Goal: one file, no server, nothing lost.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 4.1 | SharpCoreDB factory and schema | Idempotent schema; missing master password yields an actionable error | `SharpCoreDbFactory` + repository tests |
| 4.2 | Account repository | Full round-trip of every property and flag | `SharpCoreDbRekeningRepositoryTests` |
| 4.3 | Entry repository | Entries and lines round-trip in order; period filter; delete removes lines | `SharpCoreDbJournaalpostRepositoryTests` |
| 4.4 | Durability | Data survives closing and reopening the database | `Data_SurvivesReopen` tests |

**Status: done.**

## Epic 5 — Reports

*Goal: statements a human can read, print and export.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 5.1 | Report registry | Four reports registered by key with label keys and templates | `ReportRegistryTests` |
| 5.2 | Razor rendering | Compiled templates render outside an HTTP request | `RazorViewToStringRenderer`, `HtmlReportRendererTests` |
| 5.3 | Self-contained HTML | Embedded styling, no JavaScript, no external URLs, print-ready | `HtmlReportRendererTests` |
| 5.4 | CSV export | RFC 4180, invariant numbers, quoted when needed | `CsvReportExporterTests` |
| 5.5 | Localized labels | All report text via `IReportLabels`; no literals in templates | `IReportLabels`, `LocalisatieTests` |

**Status: done.**

## Epic 6 — Web application

*Goal: a small business can actually use it.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 6.1 | Host composition | Storage, application and reports wired from configuration | `Program.cs` + smoke test |
| 6.2 | Dashboard | Three statements summarised plus ratios for a chosen year | Smoke test (HTTP 200, localized content) |
| 6.3 | Chart of accounts page | List and create accounts | Manual |
| 6.4 | Journal entries page | List per year and post a two-line entry | Manual |
| 6.5 | Report viewer | Renders the selected report, CSV download, print | Smoke test (`/Rapport?handler=Html`) |
| 6.6 | Friendly failures | Expected errors show a message, not a stack trace | `FinPageModel` |

**Status: done.**

## Epic 7 — Localization

*Goal: usable across Europe, Dutch first.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 7.1 | Eight resource files | nl (neutral), en, de, fr, es, it, pt, pl | `Resources/SharedResource*.resx` |
| 7.2 | Request localization | `?culture=xx`, cookie persistence, browser header fallback | `Program.cs` middleware |
| 7.3 | Report labels bound to the localizer | Reports follow the request culture | `LocalizedReportLabels` |
| 7.4 | Completeness test | Every key resolves in every supported culture | `IedereTaal_BevatAlleSleutels` |

**Status: done.**

## Epic 8 — OSS delivery

*Goal: someone else can clone, build, trust and contribute.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 8.1 | Solution scaffold | `.slnx`, pinned SDK, shared build props, editorconfig, gitignore | Repository root |
| 8.2 | Warning-free build | Microsoft + Sonar analyzers clean | CI build step |
| 8.3 | Cross-platform CI | Restore → build (warnings as errors) → test → pack on Linux, Windows, macOS | `.github/workflows/ci.yml` |
| 8.4 | Dependency audit | Known vulnerable packages surface in the pipeline | CI `security` job |
| 8.5 | Documentation | README, CONTRIBUTING and the four BMAD artifacts | `README.md`, `CONTRIBUTING.md`, `docs/` |
| 8.6 | License and metadata | MIT license; package metadata on the libraries | `LICENSE`, `Directory.Build.props` |

**Status: done.**

## Epic 9 — Bank import and the matching assistant

*Goal: get a real bank statement into the ledger without typing it over.*

| ID | Story | Done when | Evidence |
| --- | --- | --- | --- |
| 9.1 | Statement formats | CAMT.053 and CSV are first-class formats, so a reader never guesses | `BankstaatFormaat` |
| 9.2 | Imported movement | Signed amount plus the bank's reference; bank account + reference form the import key | `BankTransactieTests` |
| 9.3 | Matching rule | Keyword, counterparty account and confirmation count, learned from a confirmed booking | `TegenrekeningRegelTests` |
| 9.4 | Matching assistant | Certainty 0-100 with a reason (IBAN 100, keyword 90, account name/number 70); reliable from 90 | `MatchingAssistentTests`, `BoekingsvoorstelTests` |
| 9.5 | Import use case | Reads a statement, skips movements that were imported before, reports read/added/skipped | `BankstaatServiceTests` |
| 9.6 | CAMT.053 reader | Namespace- and version-agnostic; booked movements only; reference fallback `AcctSvcrRef` → `NtryRef` → `EndToEndId` → `TxId` → `InstrId` | `Camt053ImporteurTests` |
| 9.7 | CSV reader | Columns found by header name, delimiter and number notation detected, quoted fields supported | `CsvImporteurTests` |
| 9.8 | Stable fallback reference | A movement without a bank reference gets a content-derived `SYN-` reference, so importing twice never duplicates | `Camt053ImporteurTests`, `CsvImporteurTests` |
| 9.9 | Storage | Movements and matching rules round-trip; one rule per keyword, so conflicting rules cannot exist | `SharpCoreDbBankstaatRepositoryTests`, `SharpCoreDbTegenrekeningRegelRepositoryTests` |
| 9.10 | Web page | Upload a statement, see the proposal per movement, book it or ignore it | `Pages/Bankimport.cshtml`, `BankimportModel` |
| 9.11 | Batch proposal | Every open movement is proposed in one pass: the ledger and the learned rules are read once per page load, not once per movement | `BankstaatServiceTests` |

**Status: done.**

## Verification log

| Gate | Result |
| --- | --- |
| `dotnet build FinOverzicht.slnx` | Succeeds — **0 warnings / 0 errors** |
| `dotnet test FinOverzicht.slnx` | **329 passed**, 0 failed, 0 skipped (146 domain, 56 application, 79 infrastructure, 26 reports, 22 architecture) |
| Live smoke test | Dashboard `200`; `?culture=en` renders "Balance sheet"; `/Rapport?handler=Html` returns a self-contained HTML document with embedded CSS |

## Learn / adjust

What the build phase taught us, and what changed as a result:

1. **SharpCoreDB block names are capped at 31 characters.** Table and index names now carry a
   comment explaining why they are short, and the schema is the single place that knows the DDL.
2. **The Razor report engine also needs `DiagnosticSource` *and* `DiagnosticListener`.** Both are
   registered with `TryAdd` so the engine works outside a web host — a real improvement over the
   original design, where only the web host could render.
3. **`ResourcesPath` and the marker type's namespace can silently double up**, making every lookup
   miss. The convention is now documented in the architecture doc and guarded by the localization
   tests, which fail loudly when a key does not resolve.
4. **Cash flow classification needed an explicit rule rather than configuration.** Deriving it from
   the counterpart account type made it testable and kept the configuration surface small.
5. **A statement without bank references cannot be identified by reference.** The import key is the
   bank account plus the bank's reference, so a movement without one now gets a content-derived
   `SYN-` reference. Re-importing the same file stays safe, at the price that two movements sharing
   their date, amount, currency and text count as one — the honest trade-off of not asking the user
   to resolve duplicates.
6. **CSV columns are found by header name, not by position.** Every bank exports its own column
   order and its own number notation; matching normalised header names (and detecting the delimiter
   per file) reads both Dutch and English exports without a bank-specific reader.
7. **A proposal per movement is an N+1 waiting to happen.** The import page first asked the assistant
   for every movement separately, so the ledger and the learned rules were read again and again for
   the same two tables. Proposals are now built in one pass (`VoorstellenAsync`), and a test asserts
   the read count so a later refactor cannot quietly bring the extra reads back.

## Next (post-1.0 backlog)

Ordered by value, not committed:

1. **VAT handling** — rates per line, a VAT return summary, and VAT payable/receivable accounts.
2. **Multiple currencies** — per-entry rate, revaluation, and a currency-aware cash flow statement.
3. **Invoice generation** — from posted entries, with PDF through a pluggable renderer.
4. **Ledger CSV/Excel round-trip** — import and export for migration and archival.
5. **Multi-administration** — one file per client, selectable, with consolidation later.
6. **Audit trail** — immutable entry history with correcting entries instead of edits.

