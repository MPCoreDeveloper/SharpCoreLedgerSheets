# 02 — Product requirements

> BMAD phase: **Plan**. Derived from [`01-brief.md`](01-brief.md).

## 1. Functional requirements

### FR-1 Ledger

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-1.1 | An account has a unique number, a name and one of five types: assets, liabilities, equity, revenue, expenses | `RekeningTests`, repository tests |
| FR-1.2 | An account can be flagged as current (short-term), as inventory, and/or as cash | `RekeningTests`, `BalansOpstellerTests` |
| FR-1.3 | A journal entry has a date, a description, an optional reference and at least two lines | `JournaalpostTests` |
| FR-1.4 | A line is either a debit or a credit — never both, never neither, never negative | `JournaalpostTests` |
| FR-1.5 | Total debit must equal total credit before an entry can be posted | `JournaalpostTests`, `GrootboekTests` |
| FR-1.6 | Posting to an unknown or deactivated account is rejected | `GrootboekTests`, `JournaalpostServiceTests` |
| FR-1.7 | The same entry cannot be posted twice | `GrootboekTests` |

### FR-2 Statements

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-2.1 | The balance sheet shows assets, liabilities, equity and the result as of a date | `BalansOpstellerTests` |
| FR-2.2 | Assets must equal liabilities + equity + result, and the statement reports whether it does | `BalansOpstellerTests` |
| FR-2.3 | Credit-normal accounts (liabilities, equity, revenue) are presented as positive amounts | `BalansOpstellerTests`, `WinstVerliesOpstellerTests` |
| FR-2.4 | Accounts without a balance (or without movement in the period) are omitted | `BalansOpstellerTests`, `WinstVerliesOpstellerTests` |
| FR-2.5 | The profit and loss statement covers a period and reports revenue, expenses and the result | `WinstVerliesOpstellerTests` |
| FR-2.6 | The cash flow statement splits movements into operating, investing and financing activities | `KasstroomOpstellerTests` |
| FR-2.7 | Cash flow classification follows the counterpart account type (revenue/expense → operating, non-cash asset → investing, liability/equity → financing) | `KasstroomOpstellerTests` |
| FR-2.8 | Entries without a cash movement do not appear in the cash flow statement | `KasstroomOpstellerTests` |
| FR-2.9 | Opening cash + net movement equals closing cash | `KasstroomOpstellerTests` |
| FR-2.10 | Ratios: current, quick, solvency, net profit margin, return on equity | `RatioBerekeningTests` |
| FR-2.11 | A ratio whose denominator is zero is reported as *not available*, never as a division error | `RatioBerekeningTests` |

### FR-3 Storage

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-3.1 | The administration is stored in one encrypted SharpCoreDB file, protected by a master password | `SharpCoreDbRekeningRepositoryTests` |
| FR-3.2 | Schema creation is idempotent and runs on every start | Infrastructure tests (reopen) |
| FR-3.3 | Data survives closing and reopening the database | `Data_SurvivesReopen` tests |
| FR-3.4 | Deleting an entry removes its lines too | `VerwijderenAsync_RemovesEntryAndItsLines` |
| FR-3.5 | A missing master password produces an actionable message, not a stack trace | `SharpCoreDbFactory` + web startup warning |

### FR-4 Reports

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-4.1 | Four reports: financial overview, balance sheet, profit and loss, cash flow | `ReportRegistryTests` |
| FR-4.2 | Reports render from Razor templates to a complete HTML document | `HtmlReportRendererTests` |
| FR-4.3 | The HTML is self-contained: embedded styling, no JavaScript, no external URLs | `HtmlReportRendererTests` |
| FR-4.4 | All report text comes from `IReportLabels`; templates hold no literals | `IReportLabels` + localization tests |
| FR-4.5 | Every report can be exported as RFC 4180 CSV with invariant numbers | `CsvReportExporterTests` |
| FR-4.6 | An unknown template fails with a clear `ReportRenderingException` | `HtmlReportRendererTests` |

### FR-5 Web application

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-5.1 | Dashboard shows the three statements summarised plus the ratios for a chosen year | Manual + smoke test |
| FR-5.2 | Accounts can be listed and created | Manual |
| FR-5.3 | Entries can be listed per year and posted as a two-line entry | Manual |
| FR-5.4 | The report viewer renders the selected report and offers CSV download and print | Manual |
| FR-5.5 | Language can be switched and the choice persists | `LocalisatieTests` + cookie middleware |
| FR-5.6 | Expected failures (domain rules, missing data) show a readable message | `FinPageModel` |

### FR-6 Localization

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-6.1 | Eight languages: nl (default), en, de, fr, es, it, pt, pl | `LocalisatieTests` |
| FR-6.2 | Every supported language defines every key | `IedereTaal_BevatAlleSleutels` |
| FR-6.3 | An unsupported culture falls back to Dutch | `OnbekendeCultuur_ValtTerugOpNederlands` |
| FR-6.4 | Numbers and dates follow the request culture | `ToString("N2")` / `"d"` with `CurrentCulture` |

## 2. Non-functional requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | `FinOverzicht.Domain` has no package references and no framework dependencies |
| NFR-2 | Layers depend inwards only; enforced by architecture tests |
| NFR-3 | The build produces zero warnings under the Microsoft + Sonar analyzers |
| NFR-4 | All tests pass on Linux, Windows and macOS in CI |
| NFR-5 | A report renders as a single HTML file that needs no network access |
| NFR-6 | No headless browser or JavaScript runtime is required to run the product |
| NFR-7 | Public API is XML-documented and named in consistent domain Dutch |
| NFR-8 | Money is always rounded to the currency's decimals; mixing currencies throws |

## 3. Design decisions worth recording

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| Domain has zero package references | Put entities in the storage project | The accounting rules are the risky part; keeping them dependency-free makes them testable and portable |
| Money as a `decimal` value object with a currency | `double` or raw `decimal` | Rounding and mixed-currency bugs are the classic accounting defect; the type prevents them |
| `Grootboek` in memory, statements derived | SQL aggregation per statement | Keeps the accounting logic in one testable place and independent of the storage engine |
| Store amounts as `DECIMAL`, dates as ISO-8601 text | Store `DateTime` | ISO text sorts chronologically and is unambiguous across cultures |
| Index and table names kept short | Descriptive long names | SharpCoreDB derives metadata block names from them and caps a block name at 31 characters |
| Cash flow classified from counterpart accounts | A configurable classification per account | Fewer knobs, an explicit documented rule, and it can be tested |
| HTML + browser print instead of server-side PDF | Bundling a headless browser | Keeps the package small and cross-platform; the HTML is already print-ready |
| Thin Razor Pages instead of a JavaScript SPA | Blazor or a front-end framework | No Node.js, no build step, server-rendered and localized by request culture |

## 4. Acceptance

A release is acceptable when:

1. All eight success criteria of the brief are demonstrated by automated tests.
2. `dotnet build FinOverzicht.slnx` and `dotnet test FinOverzicht.slnx` are green and warning-free on
   all three CI operating systems.
3. The README lets a new user go from clone to a visible balance sheet without reading source code.

