# LedgerSheets

**The three core financial statements — balance sheet, profit & loss and cash flow — for small
businesses and individuals, in one small, dependency-light .NET package.**

LedgerSheets is an embedded accounting engine and web application built on
[SharpCoreDB](https://github.com/MPCoreDeveloper/SharpCoreDB): a double-entry ledger is the single
source of truth, and every statement plus the key ratios are derived from it. Data is stored in a
single encrypted `.scdb` file that never leaves your machine.

[![CI](https://github.com/MPCoreDeveloper/LedgerSheets/actions/workflows/ci.yml/badge.svg)](https://github.com/MPCoreDeveloper/LedgerSheets/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 11](https://img.shields.io/badge/.NET-11-blue.svg)](https://dotnet.microsoft.com/download)
[![C# 15](https://img.shields.io/badge/C%23-15-purple.svg)](https://learn.microsoft.com/dotnet/csharp/)

---

## Why

Small entrepreneurs and consumers rarely need a full ERP. They need to answer three questions — and
have them stay consistent with each other:

1. **What do I own and owe?** — the balance sheet (*balans*)
2. **Did I make a profit?** — the profit and loss statement (*winst- en verliesrekening*)
3. **Where did my cash go?** — the cash flow statement (*kasstroomoverzicht*)

LedgerSheets answers those from **one** double-entry ledger. The statements cannot drift apart
because they are three views of the same journal entries, and the engine asserts that they
reconcile (assets = liabilities + equity, and opening cash + movement = closing cash).

## Highlights

- **Double-entry, enforced.** Journal entries must balance; a line is either debit or credit, never
  both. Invalid entries are rejected with a clear domain exception.
- **Three statements + ratios** derived from the ledger: current ratio, quick ratio, solvency, net
  profit margin and return on equity.
- **Reconciliation is asserted**, not assumed: the balance sheet balances and the cash flow
  statement ties opening to closing cash.
- **Encrypted, embedded storage** via SharpCoreDB — one file, no server to run.
- **Reports** rendered from Razor templates into self-contained, print-ready HTML, plus CSV export
  for spreadsheets.
- **Localized** in Dutch (default), English, German, French, Spanish, Italian, Portuguese and
  Polish — report labels come from `IReportLabels`, so templates stay language-neutral.
- **Bank statements come in as they are** — CAMT.053 (XML) or CSV, with a matching assistant that
  proposes a counterparty account per movement and learns from the bookings you confirm.
- **Architecture tests** guard the layering, and the build is warning-free under the Microsoft and
  Sonar analyzers.

## Architecture

```
LedgerSheets.Web            ASP.NET Core (Razor Pages) + localization
      |
      +-- LedgerSheets.Reports         Razor report templates, registry, CSV export
      |
      +-- LedgerSheets.Infrastructure  SharpCoreDB repositories + CAMT.053/CSV statement readers
      |
      +-- LedgerSheets.Application     use cases: account/entry services, reporting service
                 |
                 +-- LedgerSheets.Domain    ledger, statements, ratios - no dependencies at all
```

Dependencies point inwards only. `LedgerSheets.Domain` has **zero** package references: the
bookkeeping rules, the three statements and the ratios are pure, deterministic and fully unit
tested. `Application` defines the storage ports, `Infrastructure` implements them with SharpCoreDB
and reads the bank statement formats, `Reports` renders, and `Web` composes.

### Domain model

| Concept | Type | Notes |
| --- | --- | --- |
| Currency | `Currency` | ISO-4217, drives the decimals of every amount |
| Money | `Amount` | Rounds to the currency's decimals; refuses mixed-currency arithmetic |
| Period | `Period` | Inclusive date range, for example `Period.ForYear(2026)` |
| Account | `Account` | Assets, liabilities, equity, revenue, expenses + classification flags |
| Entry | `JournalEntry` / `JournalEntryLine` | Balanced debit/credit lines |
| Ledger | `Ledger` | Accounts plus entries; the single source of truth |
| Statements | `BalanceSheet`, `ProfitAndLoss`, `CashFlowStatement`, `RatioOverview` | Immutable results |
| Builders | `BalanceSheetBuilder`, `ProfitAndLossBuilder`, `CashFlowBuilder`, `RatioCalculator` | Pure domain services |
| Bank movement | `BankTransaction` | Signed movement with the bank's reference; account + reference form the import key |
| Matching rule | `CounterAccountRule` | Keyword → counterparty account, learned from a confirmed booking |
| Proposal | `PostingProposal` | Suggested counterparty account with a certainty (0-100) and the reason |

Cash flow classification is derived, not configured: an entry touching a cash account is
*operating* when its counterpart is revenue or an expense, *investing* when the counterpart is a
non-cash asset, and *financing* when the counterpart is a liability or equity.

## Quick start

```bash
git clone https://github.com/MPCoreDeveloper/LedgerSheets.git
cd LedgerSheets
dotnet build LedgerSheets.slnx
dotnet test  LedgerSheets.slnx
```

Run the web app (the database is encrypted, so it needs a master password):

```bash
cd src/LedgerSheets.Web
dotnet user-secrets init
dotnet user-secrets set "LedgerSheets:Storage:MasterPassword" "choose-a-strong-password"
dotnet run
```

Then open the dashboard, add accounts under **Accounts**, post entries under **Journal**, import
a bank statement under **Bank import** and view the statements under **Report**.

> The master password can also be supplied through the environment variable
> `LedgerSheets__Storage__MasterPassword`. **Losing it means losing the data** — LedgerSheets never
> stores it anywhere.

### Using the engine as a library

```csharp
using LedgerSheets.Domain.Accounting;
using LedgerSheets.Domain.Services;

var ledger = new Ledger();

var cash    = ledger.AddAccount("1000", "Cash", AccountType.Assets, isCurrent: true, isCashEquivalent: true);
var revenue = ledger.AddAccount("8000", "Revenue", AccountType.Revenue);

ledger.Post(JournalEntry.New(new DateOnly(2026, 6, 15), "Cash sale")
    .Debit(cash.Id, 250m)
    .Credit(revenue.Id, 250m));

var period  = Period.ForYear(2026);
var balance = new BalanceSheetBuilder().BuildOn(ledger, period.Closing);
var ratio   = new RatioCalculator().Calculate(balance, new ProfitAndLossBuilder().BuildOn(ledger, period));

Console.WriteLine($"Assets: {balance.TotalAssets:N2} (balanced: {balance.IsBalanced})");
Console.WriteLine($"Current ratio: {ratio.CurrentRatio:N2}");
```

## Reports

| Report | Template | Contents |
| --- | --- | --- |
| Financial overview | `FinancialOverviewReport` | All three statements plus the ratios in one document |
| Balance sheet | `BalanceSheetReport` | Assets, liabilities, equity and the balance check |
| Profit & loss | `ProfitAndLossReport` | Revenue, expenses and the result |
| Cash flow | `CashFlowReport` | Operating, investing and financing activities with the reconciliation |

Reports are resolved through `ReportRegistry`, rendered by `IReportRenderer` and exported with
`IReportExporter` (RFC 4180 CSV, invariant numbers, ready for any spreadsheet).

The rendered HTML is deliberately **self-contained**: styling is embedded, there is no JavaScript
and no CDN. Print it (or use the *Print* link in the web app) to get a PDF from the browser — which
avoids shipping a headless browser inside a small package. Register a custom `IReportRenderer` if
you need server-side PDF generation.

## Bank import

Import a bank statement under **Bank import**: choose the ledger account it belongs to, upload a
CAMT.053 (XML) or CSV file, and LedgerSheets reads it. The reader follows the format rather than the
bank: XML elements are matched on their local name (so every CAMT.053 version and namespace works)
and CSV columns are recognised by their header names, with the delimiter and the number notation
detected. Only **booked** movements are read — a pending movement can still change.

The matching assistant then proposes a counterparty account per movement and says why: an IBAN you
booked before counts as certain (100), a keyword you confirmed scores 90, and an account whose name
or number appears in the movement scores 70. From 90 the proposal is pre-selected; below that it is
shown as a hint. Confirmations are remembered as rules, so the same counterparty is recognised next
month — the assistant never invents a rule on its own. All proposals are built in one pass: the
ledger and the learned rules are read once per page, however many movements the statement holds.

Importing is **idempotent**: a movement is identified by the bank account plus the bank's own
reference, so re-importing the same statement adds nothing. When a bank supplies no reference at all,
LedgerSheets derives one from the content of the movement (`SYN-…`); two movements that share their
date, amount, currency and text then count as one.

## Localization

Dutch is the default and the neutral fallback. Translations ship for English, German, French,
Spanish, Italian, Portuguese and Polish in
`src/LedgerSheets.Web/Resources/SharedResource.*.resx`. Switch language in the web app (top right)
or with `?culture=de`; the choice is remembered in a cookie.

Adding a language means adding one `.resx` file and one entry in `Languages.Supported` — the
localization tests then verify that every key is present.

## Testing

```bash
dotnet test LedgerSheets.slnx
```

| Suite | Focus |
| --- | --- |
| `LedgerSheets.Domain.Tests` | Value objects, ledger invariants, statement maths, ratios, bank movements and matching |
| `LedgerSheets.Application.Tests` | Use cases against in-memory repositories |
| `LedgerSheets.Infrastructure.Tests` | Real encrypted SharpCoreDB round-trips, reopen durability and the CAMT.053/CSV readers |
| `LedgerSheets.Reports.Tests` | Report registry, CSV export and full Razor rendering |
| `LedgerSheets.ArchitectureTests` | Layer dependencies, naming and localization completeness |

## Requirements

- .NET SDK **11.0** (the solution targets `net11.0` / C# 15)
- No database server, no Node.js, no design-time tooling

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). The decisions behind the code are documented as BMAD
artifacts in [`docs/`](docs/): brief, PRD, architecture and epics.

## License

[MIT](LICENSE) © MPCoreDeveloper

