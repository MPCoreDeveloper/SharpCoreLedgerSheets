# 03 — Architecture

> BMAD phase: **Plan** (technical design). Implements [`02-prd.md`](02-prd.md) on .NET 11 / C# 15.

## 1. Layering

```
LedgerSheets.Web                ASP.NET Core host (Razor Pages, localization, DI composition)
  |
  +-- LedgerSheets.Reports      Razor templates, ReportRegistry, IReportRenderer, CSV exporter
  |
  +-- LedgerSheets.Infrastructure   SharpCoreDB adapters + CAMT.053/CSV statement readers
  |
  +-- LedgerSheets.Application      use cases + port definitions (IAccountRepository, ...)
             |
             +-- LedgerSheets.Domain    entities, value objects, statements, ratios (no dependencies)
```

Dependency direction is tested, not merely intended: `LaagAfhankelijkhedenTests` fails the build if
the domain reaches for ASP.NET, Microsoft.Extensions or SharpCoreDB, or if any layer reaches
outwards.

### Why an in-memory ledger?

Statements are computed from a rehydrated `Ledger` rather than from SQL aggregates. The ledger
of a small administration is tiny, and this choice buys three things:

1. the accounting rules live in exactly one place and are unit-testable without a database;
2. the storage engine is replaceable (the ports are `IAccountRepository` / `IJournalEntryRepository`);
3. a report needs history anyway — cumulative equity and the cash *opening* balance require entries
   older than the reporting period.

## 2. Domain model

| Type | Kind | Responsibility |
| --- | --- | --- |
| `Currency` | value object | ISO-4217 code, symbol, decimals |
| `Amount` | value object (struct) | Rounded money; refuses mixed-currency arithmetic |
| `Period` | value object (struct) | Inclusive date range with `Bevat` and `VoorJaar` |
| `Account` | entity | An account and its classification flags |
| `JournalEntry` / `JournalEntryLine` | entity | A balanced entry and its lines |
| `Ledger` | aggregate | Accounts + entries; `Boek`, `Herstel`, `Saldo`, `MutatieInPeriod` |
| `BalanceSheet`, `ProfitAndLoss`, `CashFlowStatement`, `RatioOverview` | immutable results | The statements and the ratios |
| `BalanceSheetBuilder`, `ProfitAndLossBuilder`, `CashFlowBuilder`, `RatioCalculator` | domain services | Derive the statements from the ledger |
| `BankTransaction` | entity | An imported bank movement: signed amount, the bank's reference, counterparty and status |
| `CounterAccountRule` | entity | A matching rule: keyword → counterparty account, with a confirmation count |
| `PostingProposal` | immutable result | The suggested counterparty account plus its certainty (0-100) and reason |
| `MatchingAssistent` | domain service | `StelVoor`: IBAN > confirmed keyword > account name/number |

### Sign convention

Every account balance is stored **signed, debit-positive**: `Debet − Credit`. Debit-normal accounts
(assets, expenses) are therefore positive when they hold value; credit-normal accounts
(liabilities, equity, revenue) are negative. The presentation layer flips the sign for
credit-normal accounts, which makes the accounting equation fall out of the cancellation of the
ledger total:

```
sum(all balances) = 0
=> Activa = Passiva + EigenVermogen + Resultaat
```

### Cash flow derivation

For every entry in the period that touches an account flagged `IsLiquideMiddel`, the cash movement
is `sum(Debet − Credit)` over those cash lines, classified by the counterpart accounts:

| Counterpart | Section |
| --- | --- |
| Revenue or expense | Operating |
| Non-cash asset | Investing |
| Liability or equity | Financing |

Movements are aggregated per section and description; entries without a cash movement are skipped.
The result reconciles by construction: opening cash plus the three section totals equals closing
cash, and `CashFlowStatement.IsBalanced` asserts it.

## 3. Application layer

| Service | Responsibility |
| --- | --- |
| `IAccountService` | List, create, rename and deactivate accounts |
| `IJournalEntryService` | List and post entries; resolves account numbers to identities |
| `ILedgerLader` | Rehydrate a complete `Ledger` from the ports |
| `IRapportageService` | Compose the statement builders into a `FinancieelOverzicht` for a period |
| `IBankstaatService` | Import a statement, list movements, propose a counterparty account, book or ignore a movement |

Ports: `IAccountRepository`, `IJournalEntryRepository`, `IBankstaatRepository`,
`ICounterAccountRuleRepository` and `IBankstaatImporteur`. The application layer never mentions the
storage engine or a bank format; `AddLedgerSheetsApplication()` registers the use cases and the
domain services.

### Bank import flow

```
BankstaatService.ImporteerAsync
    -> IBankstaatImporteur (first reader whose KanVerwerken says yes)
    -> BankTransaction per movement, keyed on bank account + bank reference
    -> IBankstaatRepository.BestaandeSleutelsAsync: duplicates are skipped, not overwritten
BankstaatService.VoorstelAsync     -> IMatchingAssistent (one movement)
BankstaatService.VoorstellenAsync  -> IMatchingAssistent (a whole list; ledger + learned rules read once)
BankstaatService.BoekAsync         -> IJournalEntryService (balanced entry) + rule upsert (confirmations + 1)
```

The import page uses the batch variant: the number of open movements drives the number of proposals,
not the number of times the ledger and the learned rules are read. A statement of a few hundred
movements therefore costs two reads, not a few hundred round trips.

`IBankstaatImporteur` is the seam that keeps formats out of the domain: a new bank format is one
class in the infrastructure layer plus one registration. Two rules are deliberate:

1. **Only booked movements.** A CAMT `PDNG` entry can still change or disappear, so booking it would
   create an entry the next statement contradicts.
2. **Import is idempotent.** A movement without a bank reference gets a content-derived `SYN-`
   reference, because identifying a movement by anything less stable would duplicate it on the next
   import of an overlapping period.

## 4. Infrastructure

`AddLedgerSheetsStorage(configure)` wires SharpCoreDB:

- `AddSharpCoreDB()` provides the crypto services and `DatabaseFactory`;
- `ISharpCoreDbFactory.Open()` validates the master password, creates the directory, opens (or
  creates) the `.scdb` file and applies the schema;
- one **singleton** `IDatabase` handle serves the application lifetime and is disposed with the
  container;
- repository calls are serialized on the database handle because the embedded engine is not
  designed for concurrent writers.

Statement readers implement `IBankstaatImporteur` and live here, not in the domain:
`Camt053Importeur` matches XML elements on their **local name** (so every CAMT.053 version and
namespace is accepted, and `DBIT`/`CRDT` decides the sign), while `CsvImporteur` finds columns by
their normalised header name and detects the delimiter and the number notation per file. Both throw
a `DomeinException` with a message that can be shown to the user as-is, and both fall back to
`SynthetischeReferentie` when the bank supplies no reference at all.

### Schema

| Table | Purpose | Key columns |
| --- | --- | --- |
| `rekeningen` | Chart of accounts | `id`, `nummer`, `naam`, `soort`, `is_actief`, `is_vlottend`, `is_liquide_middel`, `is_voorraad` |
| `posten` | Journal entries | `id`, `datum` (ISO-8601 text), `omschrijving`, `referentie` |
| `postregels` | Entry lines | `id`, `post_id`, `volgnummer`, `rekening_id`, `debet`, `credit`, `omschrijving` |
| `bankmutaties` | Imported bank movements | `id`, `bank_nr`, `datum`, `bedrag`, `valuta`, `referentie`, `tegenpartij`, `iban`, `omschrijving`, `status`, `post_id` |
| `matchregels` | Matching rules learned from confirmed bookings | `trefwoord` (unique), `tegenrekening`, `bevestigingen` |

Amounts are `DECIMAL`; dates are `yyyy-MM-dd` text so they sort chronologically. **Every table and
index name is deliberately short**: SharpCoreDB derives metadata block names such as
`table:<table>:overflow` and caps a block name at 31 characters.

Rehydration uses `Ledger.Herstel` rather than `Boek`: historical entries may reference accounts
that have since been deactivated, so the "account must be active" rule applies when posting, not
when loading. Balance and account existence are still enforced on load.

## 5. Reports

```
ReportRegistry  ->  IReportRenderer (HtmlReportRenderer -> Razor templates)
                ->  IReportExporter (CsvReportExporter)
```

`RazorViewToStringRenderer` renders a compiled `.cshtml` outside an HTTP request using a synthetic
`ActionContext`; `ReportViewLocationExpander` teaches the view engine about `/Templates/`.
Everything a template needs — styling, labels, header data — arrives through `ViewData`, so the
layout never touches `Model` and templates never hard-code a word. `IReportLabels` is the seam that
keeps the engine language-neutral: the web app binds it to `IStringLocalizer`, and a test can bind
it to anything.

`AddLedgerSheetsReports()` also registers `DiagnosticSource`/`DiagnosticListener` with `TryAdd`, so
the engine renders inside a bare service provider (tests, console tools) as well as inside a full
web host.

Reports are HTML + CSV. PDF comes from the browser's print dialog on the already print-ready HTML,
which keeps a headless browser out of the package; register a custom `IReportRenderer` when
server-side PDF is genuinely required.

## 6. Web layer

Razor Pages, server-rendered, with `AddLocalization()` and request localization configured for the
eight supported cultures. `QueryStringRequestCultureProvider` handles `?culture=de`, a cookie
provider remembers the choice, and a short middleware writes that cookie with `HttpOnly`, `Secure`
and `SameSite=Lax`. `FinPageModel` converts expected domain/storage failures into readable messages
instead of stack traces.

Pages stay thin: `Accounten` and `Boekingen` pass what they are given to a service, and `Bankimport`
uploads a statement, shows the proposal per open movement and offers *book* or *ignore* — the
assistant decides nothing on its own, the user always confirms.

The `SharedResource` marker type lives in `Resources/` next to its `.resx` files, which is exactly
what the default `ResourceManagerStringLocalizer` naming convention expects — so
`IStringLocalizer<SharedResource>` resolves `LedgerSheets.Web.Resources.SharedResource[.<culture>]`.

## 7. Testing strategy

| Layer | Approach |
| --- | --- |
| Domain | Pure unit tests over a shared ledger scenario (`TestLedger`) with hand-computed expectations; bank movements, matching rules and the assistant are covered the same way |
| Application | Use cases against in-memory repositories that implement the ports |
| Infrastructure | Real encrypted SharpCoreDB databases in the temp folder, including reopen durability, plus the CAMT.053/CSV readers against real statement fragments |
| Reports | Full Razor rendering through a real service provider, plus CSV assertions |
| Cross-cutting | NetArchTest layer rules, naming rules and localization completeness |

Three rules keep the suites honest:

1. **Hand-computed expectations.** The domain scenarios assert concrete totals (18 300 / 6 000 /
   10 000 / 2 300) rather than recomputing them with the code under test.
2. **Round-trip, then reopen.** Storage tests always close and reopen the database before asserting,
   because persistence bugs hide behind a warm cache.
3. **Render, don't mock.** Report tests run the real Razor pipeline; mocking the view engine would
   prove nothing about the templates.

Test infrastructure stays in the test assemblies; production code carries no test hooks.

