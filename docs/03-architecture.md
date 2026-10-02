# 03 — Architecture

> BMAD phase: **Plan** (technical design). Implements [`02-prd.md`](02-prd.md) on .NET 11 / C# 15.

## 1. Layering

```
FinOverzicht.Web                ASP.NET Core host (Razor Pages, localization, DI composition)
  |
  +-- FinOverzicht.Reports      Razor templates, ReportRegistry, IReportRenderer, CSV exporter
  |
  +-- FinOverzicht.Infrastructure   SharpCoreDB adapters implementing the Application ports
  |
  +-- FinOverzicht.Application      use cases + port definitions (IRekeningRepository, ...)
             |
             +-- FinOverzicht.Domain    entities, value objects, statements, ratios (no dependencies)
```

Dependency direction is tested, not merely intended: `LaagAfhankelijkhedenTests` fails the build if
the domain reaches for ASP.NET, Microsoft.Extensions or SharpCoreDB, or if any layer reaches
outwards.

### Why an in-memory ledger?

Statements are computed from a rehydrated `Grootboek` rather than from SQL aggregates. The ledger
of a small administration is tiny, and this choice buys three things:

1. the accounting rules live in exactly one place and are unit-testable without a database;
2. the storage engine is replaceable (the ports are `IRekeningRepository` / `IJournaalpostRepository`);
3. a report needs history anyway — cumulative equity and the cash *opening* balance require entries
   older than the reporting period.

## 2. Domain model

| Type | Kind | Responsibility |
| --- | --- | --- |
| `Valuta` | value object | ISO-4217 code, symbol, decimals |
| `Bedrag` | value object (struct) | Rounded money; refuses mixed-currency arithmetic |
| `Periode` | value object (struct) | Inclusive date range with `Bevat` and `VoorJaar` |
| `Rekening` | entity | An account and its classification flags |
| `Journaalpost` / `JournaalpostRegel` | entity | A balanced entry and its lines |
| `Grootboek` | aggregate | Accounts + entries; `Boek`, `Herstel`, `Saldo`, `MutatieInPeriode` |
| `Balans`, `WinstVerlies`, `KasstroomOverzicht`, `RatioOverzicht` | immutable results | The statements and the ratios |
| `BalansOpsteller`, `WinstVerliesOpsteller`, `KasstroomOpsteller`, `RatioBerekening` | domain services | Derive the statements from the ledger |

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
cash, and `KasstroomOverzicht.IsInBalans` asserts it.

## 3. Application layer

| Service | Responsibility |
| --- | --- |
| `IRekeningService` | List, create, rename and deactivate accounts |
| `IJournaalpostService` | List and post entries; resolves account numbers to identities |
| `IGrootboekLader` | Rehydrate a complete `Grootboek` from the ports |
| `IRapportageService` | Compose the statement builders into a `FinancieelOverzicht` for a period |

Ports: `IRekeningRepository`, `IJournaalpostRepository`. The application layer never mentions the
storage engine; `AddFinOverzichtApplication()` registers the use cases and the domain services.

## 4. Infrastructure

`AddFinOverzichtOpslag(configure)` wires SharpCoreDB:

- `AddSharpCoreDB()` provides the crypto services and `DatabaseFactory`;
- `ISharpCoreDbFactory.Open()` validates the master password, creates the directory, opens (or
  creates) the `.scdb` file and applies the schema;
- one **singleton** `IDatabase` handle serves the application lifetime and is disposed with the
  container;
- repository calls are serialized on the database handle because the embedded engine is not
  designed for concurrent writers.

### Schema

| Table | Purpose | Key columns |
| --- | --- | --- |
| `rekeningen` | Chart of accounts | `id`, `nummer`, `naam`, `soort`, `is_actief`, `is_vlottend`, `is_liquide_middel`, `is_voorraad` |
| `posten` | Journal entries | `id`, `datum` (ISO-8601 text), `omschrijving`, `referentie` |
| `postregels` | Entry lines | `id`, `post_id`, `volgnummer`, `rekening_id`, `debet`, `credit`, `omschrijving` |

Amounts are `DECIMAL`; dates are `yyyy-MM-dd` text so they sort chronologically. **Every table and
index name is deliberately short**: SharpCoreDB derives metadata block names such as
`table:<table>:overflow` and caps a block name at 31 characters.

Rehydration uses `Grootboek.Herstel` rather than `Boek`: historical entries may reference accounts
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

`AddFinOverzichtReports()` also registers `DiagnosticSource`/`DiagnosticListener` with `TryAdd`, so
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

The `SharedResource` marker type lives in `Resources/` next to its `.resx` files, which is exactly
what the default `ResourceManagerStringLocalizer` naming convention expects — so
`IStringLocalizer<SharedResource>` resolves `FinOverzicht.Web.Resources.SharedResource[.<culture>]`.

## 7. Testing strategy

| Layer | Approach |
| --- | --- |
| Domain | Pure unit tests over a shared ledger scenario (`TestGrootboek`) with hand-computed expectations |
| Application | Use cases against in-memory repositories that implement the ports |
| Infrastructure | Real encrypted SharpCoreDB databases in the temp folder, including reopen durability |
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

