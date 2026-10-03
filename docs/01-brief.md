# 01 — Product brief

> BMAD phase: **Clarify**. This is the artifact that was agreed before any code was written.
> Every later artifact (PRD, architecture, epics) traces back to the problem statement here.

## 1. Problem

Small entrepreneurs and private individuals who keep their own books fall into one of two camps:

- they use a heavyweight accounting package with hundreds of features they never touch, or
- they use a spreadsheet that cannot tell them whether their balance sheet actually balances.

Both miss the point. A small administration needs exactly three statements — balance sheet,
profit and loss, cash flow — and it needs them to be **consistent with each other**. A spreadsheet
lets you compute three numbers that quietly contradict each other; a full ERP charges you for the
privilege of features you do not need.

There is also a trust problem: cloud accounting means someone else's server holds your figures.
Many small businesses would rather keep a single, encrypted file on their own machine.

## 2. Target users

| Persona | Situation | What they need |
| --- | --- | --- |
| **Sanne, freelancer** | One-person consultancy, invoices and expenses | Enter transactions quickly, see profit and what she owes the tax office |
| **Bram, small shop owner** | Retail, has inventory and a bank loan | A real balance sheet, insight into cash and inventory, liquidity ratios |
| **Ada, bookkeeper** | Handles a handful of small clients | A small, predictable tool per client; exportable data; no vendor lock-in |
| **Daan, hobby developer** | Wants to embed bookkeeping in his own app | A library with a clean domain model, not a monolith |

## 3. Value proposition

> One encrypted file. One double-entry ledger. The three financial statements, always consistent,
> in eight European languages, with no server to run and no subscription to pay.

## 4. Scope

**In scope (v1.0)**

- Double-entry ledger: accounts, journal entries, periods.
- The three core statements, all derived from the ledger.
- Key ratios: current ratio, quick ratio, solvency, net profit margin, return on equity.
- Reconciliation checks: the balance sheet balances; the cash flow statement ties opening to
  closing cash.
- Encrypted embedded storage (SharpCoreDB, single `.scdb` file).
- Reports as self-contained, print-ready HTML plus CSV export.
- Web application: dashboard, chart of accounts, journal entries, bank import, report viewer.
- Localization: Dutch (default), English, German, French, Spanish, Italian, Portuguese, Polish.
- OSS delivery: MIT license, CI on Linux/Windows/macOS, architecture tests, contribution guide.

**Out of scope (v1.0)**

- Multiple currencies within one ledger and exchange-rate revaluation.
- VAT/tax returns, invoice generation, payroll.
- Consolidation of several administrations.
- Multi-user access, roles and audit trails.
- Server-side PDF generation (print-to-PDF from the browser covers it).
- Mobile applications.

## 5. Success criteria

| # | Criterion | How it is verified |
| --- | --- | --- |
| S1 | A posted journal entry that is not in balance is rejected | Domain tests |
| S2 | Assets equal liabilities + equity for any valid ledger | Domain tests + `Balans.IsInBalans` |
| S3 | Opening cash + net movement equals closing cash | Domain tests + `KasstroomOverzicht.IsInBalans` |
| S4 | The same ledger yields three consistent statements | Application tests over a seeded scenario |
| S5 | Data survives closing and reopening the database | Infrastructure tests |
| S6 | Reports render as self-contained HTML with no JS or CDN | Report rendering tests |
| S7 | All eight languages resolve every UI/report key | Localization tests |
| S8 | The build is warning-free and the layering holds | CI + architecture tests |

## 6. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| The encrypted database's master password is lost | Document loudly that the password is the only key; never store it; surface a clear startup warning when it is missing |
| Cash-flow classification is inherently judgemental | Make the rule explicit and derived (counterpart account type), document it, and keep it testable rather than configurable |
| Rewriting a spreadsheet tool's mental model is hard for users | Keep the vocabulary and the statements conventional; use standard accounting terms |
| Embedded storage limits concurrency | One administration per file, single long-lived connection, serialized writes — documented as a design constraint |
| Scope creep towards a full ERP | The out-of-scope list above is the contract; changes go through this brief first |

## 7. Constraints and principles

- **.NET 11 / C# 15**, the newest stable line, and **SharpCoreDB** for storage (own ecosystem).
- **SOLID and dependency inversion**: the domain knows nothing about storage, rendering or HTTP.
- **Test-driven**: the accounting rules are the highest-risk area, so they carry the densest tests.
- **Zero warnings** under the Microsoft and Sonar analyzers.
- **Cross-platform**: the same commands must work on Linux, Windows and macOS.
- **Small**: no headless browser, no JavaScript framework, no design-time tooling.
