# Feature Specification: SharpCoreLedgerSheets 1.0 — the three financial statements from one ledger

**Feature Branch**: `main` (shipped as SharpCoreLedgerSheets 2.1.0)

**Created**: 2026-11-08

**Status**: Implemented

**Input**: Migrated from the project brief and product requirements that were agreed before any code
was written. Technical design lives in `plan.md`; the decisions and the lessons learned live in
`research.md`; the work breakdown lives in `tasks.md`.

## Context

**The problem.** Small entrepreneurs and private individuals who keep their own books fall into one of
two camps: a heavyweight accounting package with hundreds of features they never touch, or a spreadsheet
that cannot tell them whether the balance sheet actually balances. A small administration needs exactly
three statements - balance sheet, profit and loss, cash flow - and it needs them to be *consistent with
each other*. There is also a trust problem: cloud accounting means someone else's server holds the
figures, and many small businesses would rather keep a single encrypted file on their own machine.

**Who it is for.**

| Persona | Situation | What they need |
| --- | --- | --- |
| Sanne, freelancer | One-person consultancy, invoices and expenses | Enter transactions quickly, see profit and what she owes the tax office |
| Bram, small shop owner | Retail, inventory and a bank loan | A real balance sheet, insight into cash and inventory, liquidity ratios |
| Ada, bookkeeper | Handles a handful of small clients | A small, predictable tool per client; exportable data; no vendor lock-in |
| Daan, hobby developer | Wants to embed bookkeeping in his own app | A library with a clean domain model, not a monolith |

**Value proposition.** One encrypted file. One double-entry ledger. The three financial statements,
always consistent, in eight European languages, with no server to run and no subscription to pay.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Post the books and read the three statements (Priority: P1)

A small business owner or freelancer keeps a chart of accounts, posts journal entries and opens the
dashboard to see a balance sheet, a profit and loss statement and a cash flow statement that agree
with each other - all from one encrypted file on their own machine.

**Why this priority**: This is the product. Without a ledger and the three statements there is
nothing to report, print or export, and no other story can be demonstrated.

**Independent Test**: Create accounts, post a handful of balanced entries, then build the overview for
a period through the engine (or the web dashboard) and verify assets equal liabilities + equity +
result and that opening cash + net movement equals closing cash.

**Acceptance Scenarios**:

1. **Given** an account `1000 Cash` (assets, current, cash equivalent) and `8000 Revenue`,
   **When** a journal entry debits cash 250 and credits revenue 250,
   **Then** the entry is accepted, the balance sheet shows 250 in assets and the P&L shows 250 revenue.
2. **Given** a journal entry whose debits total 250 and credits total 200,
   **When** the entry is posted, **Then** it is rejected with a `DomainException` and nothing changes.
3. **Given** an entry that is balanced, **When** the same entry is posted a second time,
   **Then** it is rejected as a duplicate.
4. **Given** posted entries, **When** the overview is built for a year,
   **Then** `BalanceSheet.IsBalanced` is true and `CashFlowStatement.IsBalanced` is true.

---

### User Story 2 - Print and export the statements (Priority: P2)

A bookkeeper selects a report, reads it in the browser, prints it to PDF and downloads the same figures
as CSV for a spreadsheet.

**Why this priority**: The numbers must leave the tool - to a client, an accountant or an archive -
and a self-contained HTML document plus RFC 4180 CSV covers that without adding a PDF engine.

**Independent Test**: Render each of the four reports for a seeded administration and assert the HTML
document is complete, styled, JavaScript-free and free of external URLs; export the same report to CSV
and assert the numbers and quoting.

**Acceptance Scenarios**:

1. **Given** a seeded ledger, **When** the balance sheet report is rendered,
   **Then** a complete HTML document with embedded styling, no `<script>` and no external URL is returned.
2. **Given** the same report, **When** it is exported to CSV,
   **Then** the file follows RFC 4180 with invariant numbers and is readable by any spreadsheet.
3. **Given** an unknown report key, **When** rendering is requested,
   **Then** a `ReportRenderingException` explains which template could not be resolved.

---

### User Story 3 - Import a bank statement and book it with the assistant (Priority: P3)

An entrepreneur uploads a CAMT.053 or CSV statement, sees a proposed counterparty account per open
movement with a certainty and a reason, and confirms the booking - or ignores the movement.

**Why this priority**: Retyping a bank statement is the most tedious part of small bookkeeping, but
the ledger and the statements (P1) must exist before an import has anywhere to go.

**Independent Test**: Import a statement fragment for a bank account, assert only booked movements are
read, assert the count of new versus already-known movements, and assert that importing the same file
twice adds nothing.

**Acceptance Scenarios**:

1. **Given** a CAMT.053 file with a namespace and a version, **When** it is imported,
   **Then** the movements are read by local element name and `DBIT`/`CRDT` decides the sign.
2. **Given** a movement without a bank reference, **When** the file is imported twice,
   **Then** a content-derived `SYN-` reference prevents a duplicate.
3. **Given** a movement whose IBAN was booked before, **When** proposals are requested,
   **Then** the counterparty account is proposed with certainty 100 and a stated reason.
4. **Given** a movement that was booked, **When** the user tries to ignore it,
   **Then** the operation is rejected, because a booked movement has an entry behind it.

---

### User Story 4 - Embed the engine in another application (Priority: P4)

A developer references the SharpCoreLedgerSheets libraries from their own .NET application and drives the
ledger through the application services without touching storage, rendering or HTTP.

**Why this priority**: The engine is the OSS contribution, but it is worth little if it can only be
used through the bundled web application.

**Independent Test**: Reference `SharpCoreLedgerSheets.Application` and `SharpCoreLedgerSheets.Infrastructure`, register
the services, post an entry and read the overview - with no web host involved.

**Acceptance Scenarios**:

1. **Given** a clean process, **When** the storage and application services are registered,
   **Then** an entry can be posted and read back after the database is closed and reopened.
2. **Given** an unknown account number, **When** an entry is posted,
   **Then** a `NotFoundException` names the account, and no entry is written.

---

### Edge Cases

- A journal entry line that is both debit and credit, negative or zero is rejected; an entry needs at
  least two lines.
- Posting to an unknown or deactivated account is rejected; loading history that references a
  deactivated account is not (rehydration uses `Ledger.Restore`, not `Ledger.Post`).
- A ratio whose denominator is zero reports *not available* instead of throwing a division error.
- Arithmetic on two different currencies throws; a report never mixes currencies silently.
- An unsupported culture falls back to Dutch instead of failing.
- A statement containing only pending (`PDNG`) movements imports nothing, and says so.
- Re-importing an overlapping period adds nothing; a movement is identified by bank account + bank
  reference (or the derived `SYN-` reference).
- A missing master password produces an actionable message at start-up, not a stack trace - and the
  password is never stored, so losing it means losing the data.

## Requirements *(mandatory)*

### Functional Requirements

#### FR-1 Ledger

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-1.1 | An account has a unique number, a name and one of five types: assets, liabilities, equity, revenue, costs | `AccountTests`, repository tests |
| FR-1.2 | An account can be flagged as current (short-term), as inventory and/or as cash equivalent | `AccountTests`, `BalanceSheetBuilderTests` |
| FR-1.3 | A journal entry has a date, a description, an optional reference and at least two lines | `JournalEntryTests` |
| FR-1.4 | A line is either a debit or a credit - never both, never neither, never negative | `JournalEntryTests` |
| FR-1.5 | Total debit must equal total credit before an entry can be posted | `JournalEntryTests`, `LedgerTests` |
| FR-1.6 | Posting to an unknown or deactivated account is rejected | `LedgerTests`, `JournalEntryServiceTests` |
| FR-1.7 | The same entry cannot be posted twice | `LedgerTests` |

#### FR-2 Statements

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-2.1 | The balance sheet shows assets, liabilities, equity and the result as of a date | `BalanceSheetBuilderTests` |
| FR-2.2 | Assets must equal liabilities + equity + result, and the statement reports whether it does | `BalanceSheetBuilderTests` |
| FR-2.3 | Credit-normal accounts (liabilities, equity, revenue) are presented as positive amounts | `BalanceSheetBuilderTests`, `ProfitAndLossBuilderTests` |
| FR-2.4 | Accounts without a balance (or without movement in the period) are omitted | `BalanceSheetBuilderTests`, `ProfitAndLossBuilderTests` |
| FR-2.5 | The profit and loss statement covers a period and reports revenue, costs and the result | `ProfitAndLossBuilderTests` |
| FR-2.6 | The cash flow statement splits movements into operating, investing and financing activities | `CashFlowBuilderTests` |
| FR-2.7 | Cash flow classification follows the counterpart account type (revenue/costs → operating, non-cash asset → investing, liability/equity → financing) | `CashFlowBuilderTests` |
| FR-2.8 | Entries without a cash movement do not appear in the cash flow statement | `CashFlowBuilderTests` |
| FR-2.9 | Opening cash + net movement equals closing cash | `CashFlowBuilderTests` |
| FR-2.10 | Ratios: current, quick, solvency, net profit margin, return on equity | `RatioCalculatorTests` |
| FR-2.11 | A ratio whose denominator is zero is reported as *not available*, never as a division error | `RatioCalculatorTests` |

#### FR-3 Storage

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-3.1 | The administration is stored in one encrypted SharpCoreDB file, protected by a master password | `SharpCoreDbAccountRepositoryTests` |
| FR-3.2 | Schema creation is idempotent and runs on every start | Infrastructure tests (reopen) |
| FR-3.3 | Data survives closing and reopening the database | `SharpCoreDbTestContext` reopen tests |
| FR-3.4 | Deleting an entry removes its lines too | `SharpCoreDbJournalEntryRepositoryTests` |
| FR-3.5 | A missing master password produces an actionable message, not a stack trace | `SharpCoreDbFactory` + web start-up warning |

#### FR-4 Reports

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-4.1 | Four reports: financial overview, balance sheet, profit and loss, cash flow | `ReportRegistryTests` |
| FR-4.2 | Reports render from Razor templates to a complete HTML document | `HtmlReportRendererTests` |
| FR-4.3 | The HTML is self-contained: embedded styling, no JavaScript, no external URLs | `HtmlReportRendererTests` |
| FR-4.4 | All report text comes from `IReportLabels`; templates hold no literals | `IReportLabels` + `LocalizationTests` |
| FR-4.5 | Every report can be exported as RFC 4180 CSV with invariant numbers | `CsvReportExporterTests` |
| FR-4.6 | An unknown template fails with a clear `ReportRenderingException` | `HtmlReportRendererTests` |

#### FR-5 Web application

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-5.1 | Dashboard shows the three statements summarised plus the ratios for a chosen year | Smoke test |
| FR-5.2 | Accounts can be listed and created | Manual |
| FR-5.3 | Entries can be listed per year and posted as a two-line entry | Manual |
| FR-5.4 | The report viewer renders the selected report and offers CSV download and print | Smoke test |
| FR-5.5 | Language can be switched and the choice persists | `LocalizationTests` + cookie middleware |
| FR-5.6 | Expected failures (domain rules, missing data) show a readable message | `FinPageModel` |
| FR-5.7 | A bank statement can be uploaded; every open movement shows the proposal and can be booked or ignored | `Bankimport` page, manual |

#### FR-6 Localization

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-6.1 | Eight languages: nl (default), en, de, fr, es, it, pt, pl | `LocalizationTests` |
| FR-6.2 | Every supported language defines every key | `EveryLanguage_ContainsAllKeys` |
| FR-6.3 | An unsupported culture falls back to Dutch | `LocalizationTests` |
| FR-6.4 | Numbers and dates follow the request culture | `ToString("N2")` / `"d"` with `CurrentCulture` |

#### FR-7 Bank import

| ID | Requirement | Verified by |
| --- | --- | --- |
| FR-7.1 | A statement (CAMT.053 XML or CSV) can be imported for a chosen ledger account | `Camt053ImporterTests`, `CsvImporterTests` |
| FR-7.2 | The reader follows the format, not the bank: XML by local name, CSV columns by header name | `Camt053ImporterTests`, `CsvImporterTests` |
| FR-7.3 | Only movements the bank has booked are imported | `Camt053ImporterTests` |
| FR-7.4 | Importing the same statement twice never duplicates a movement | `BankStatementServiceTests`, `CsvImporterTests` |
| FR-7.5 | The assistant proposes a counterparty account with a certainty and a reason; from 90 it is pre-selected | `MatchingAssistantTests`, `PostingProposalTests` |
| FR-7.6 | The assistant only learns from confirmed bookings, and each rule counts its confirmations | `BankStatementServiceTests`, `CounterAccountRuleTests` |
| FR-7.7 | A movement can be booked as a balanced entry or ignored; a booked movement cannot be ignored | `BankStatementServiceTests` |
| FR-7.8 | Movements and matching rules survive closing and reopening the database | `SharpCoreDbBankStatementRepositoryTests`, `SharpCoreDbCounterAccountRuleRepositoryTests` |
| FR-7.9 | Every proposal for the open movements is built in one pass: the ledger and the learned rules are read once per page load, not once per movement | `BankStatementServiceTests` |

### Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | `SharpCoreLedgerSheets.Domain` has no package references and no framework dependencies |
| NFR-2 | Layers depend inwards only; enforced by architecture tests |
| NFR-3 | The build produces zero warnings under the Microsoft + Sonar analyzers |
| NFR-4 | All tests pass on Linux, Windows and macOS in CI |
| NFR-5 | A report renders as a single HTML file that needs no network access |
| NFR-6 | No headless browser or JavaScript runtime is required to run the product |
| NFR-7 | Public API is XML-documented and consistently named |
| NFR-8 | Money is always rounded to the currency's decimals; mixing currencies throws |

### Key Entities

- **Account**: a chart-of-accounts entry with a unique number, a name, one of five types and the
  current / inventory / cash-equivalent flags.
- **JournalEntry / JournalEntryLine**: a dated, described, optionally referenced set of at least two
  lines; each line is a debit xor a credit against one account.
- **Ledger**: the aggregate of accounts and entries; the single source of truth from which every
  statement is derived.
- **BalanceSheet, ProfitAndLoss, CashFlowStatement, RatioOverview**: immutable statement results,
  each able to say whether it reconciles.
- **BankTransaction**: an imported bank movement with a signed amount, the bank's own reference,
  counterparty details and a status (open, booked, ignored).
- **CounterAccountRule**: a learned matching rule (keyword → counterparty account) with a
  confirmation count.
- **PostingProposal**: the suggested counterparty account for a movement, with a certainty (0-100)
  and the reason for it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A posted journal entry that is not in balance is rejected - proven by domain tests.
- **SC-002**: For any valid ledger, assets equal liabilities + equity + result - proven by domain tests
  and `BalanceSheet.IsBalanced`.
- **SC-003**: Opening cash + net movement equals closing cash - proven by domain tests and
  `CashFlowStatement.IsBalanced`.
- **SC-004**: One ledger yields three consistent statements for a seeded scenario - proven by the
  application tests.
- **SC-005**: Data survives closing and reopening the database - proven by the infrastructure tests.
- **SC-006**: Reports render as self-contained HTML with no JavaScript and no CDN - proven by the
  report rendering tests.
- **SC-007**: All eight languages resolve every UI and report key - proven by the localization tests.
- **SC-008**: The build is warning-free and the layering holds - proven by CI and the architecture
  tests.
- **SC-009**: Re-importing an identical bank statement adds zero movements - proven by the bank import
  tests.
- **SC-010**: The full suite (330 tests) runs in seconds and needs no database server, network or
  Node.js.

## Assumptions

- **Target users**: a small business owner, freelancer or bookkeeper with one administration of at most
  a few thousand entries per year. Volume is small, so statements are computed from an in-memory
  ledger rather than through SQL aggregation.
- **One administration per file**: the encrypted `.scdb` file *is* the administration; multi-client
  bookkeeping means multiple files.
- **Single user, serialized writes**: the embedded engine is not built for concurrent writers, so one
  long-lived database handle serializes writes. Multi-user access is out of scope for v1.0.
- **The master password is the only key**: it is never stored, so losing it means losing the data.
  This assumption is surfaced in the UI and in the README rather than worked around.
- **Default currency**: EUR, with other ISO-4217 currencies supported per ledger; multiple currencies
  inside one ledger and revaluation are out of scope.
- **Bank formats**: CAMT.053 (XML) and CSV are enough for the target users; a new format is one
  importer class in the infrastructure layer.
- **PDF is a browser feature**: printing the print-ready HTML is the documented route to PDF, which
  keeps a headless browser out of the product.
- **Out of scope for v1.0**: VAT/tax returns, invoice generation, payroll, consolidation of several
  administrations, multi-user access and audit trails, server-side PDF generation and mobile apps.
  The post-1.0 backlog is ordered in `tasks.md`.
