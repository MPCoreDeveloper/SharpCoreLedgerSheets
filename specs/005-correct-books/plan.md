# Implementation Plan: Correct books from day one

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [`spec.md`](spec.md)

## Summary

Add an administration aggregate (company, book year, VAT scheme) with its own storage, post opening balances
as one reserved journal entry, and book VAT as ordinary ledger lines to per-rate VAT accounts - with a return
that reads those accounts back. No new dependency; the domain stays pure.

## Technical Context

**Language/Version**: unchanged - C# 15 on .NET 11

**Primary Dependencies**: unchanged - SharpCoreDB 2.1.2 remains the only runtime package

**Storage**: a new `settings` table (`name`/`content` key-value rows; VAT rates as JSON) created idempotently
next to the existing schema, so 2.1 files keep opening. No change to `accounts`, `entries`, `entry_lines`,
`banktxns` or `matchrules`.

**Testing**: xunit.v3 on Microsoft.Testing.Platform; **410 tests** (62 new). Domain mutation score **75.12 %**.

**Scale/Scope**: 1 new domain namespace (`Administration`), 2 new application services, 1 repository, 2 web
pages, 1 new VAT report path, 22 new UI keys × 8 cultures, 8 new message codes × 2 catalogues.

## Design

### Settings as an aggregate
`AdministrationSettings` (Domain) holds company name, `FiscalYearStartMonth` (1..12), `Currency` and
`VatScheme`; it validates on construction and exposes `PeriodFor(year)`. Persisted by
`SharpCoreDbSettingsRepository` as key/value rows - a new setting never needs a schema change, and unreadable
values fall back to the defaults instead of bricking the administration.

### Opening balances as a reserved entry (decision)
Instead of a separate `opening_balances` table plus special cases in three statement builders, the use case
posts **one journal entry** dated `bookYear.Start - 1 day`, tagged `OPENING-<year>`:

```
Debit  1000 Kas            2 500
Credit 4000 Eigen vermogen 2 500      (reference: OPENING-2026, date: 2025-12-31)
```

Why: the ledger stays the single source of truth, the statements pick the position up without new code
(cash flow opens correctly, the balance sheet balances), the entry is visible and auditable in the journal,
and replacing it is a delete-and-post of one entry. The cost is that opening balances are journal data
rather than configuration data - recorded in `research.md`.

### VAT as ledger lines, not schema
A VAT invoice is three ordinary lines: net to revenue/cost, VAT to the rate's account, gross to the
counterparty. `VatService` composes them from a net amount, a rate and two account numbers; `VatScheme` maps
each rate to its output and input account (`21 -> 2100/1110`, `9 -> 2101/1111`, `0 -> none`). The starter
charts gained those four accounts, so the mapping works out of the box.

`VatReturnBuilder` reads the return from the VAT accounts: output VAT is the inverse of the (credit-normal)
liability movement, input VAT the (debit-normal) asset movement, per rate, for the period chosen. Because the
numbers are read back from the ledger, the return always agrees with it.

## Constitution Check

| Principle | Gate | Result |
| --- | --- | --- |
| I. A pure domain | `AdministrationSettings`, `VatScheme`, `VatReturn`, `VatReturnBuilder` have no dependencies | PASS |
| II. Inward dependencies | Ports in Application, adapter in Infrastructure, pages in Web | PASS |
| III. Tests are the evidence | 62 new tests, hand-computed VAT and opening-balance expectations | PASS - 410 green |
| IV. Warning-free build | `-warnaserror` clean (Sonar's cancellation rule included) | PASS |
| V. Small, offline, encrypted | No new package; same single encrypted file | PASS |
| VI. Reconcile, never assume | Opening balances make the balance sheet balance from day one; the VAT return is read from the ledger | PASS |
| VII. Cross-platform parity | Nothing platform-specific | PASS |
| VIII. Dutch first, localized everywhere | 22 new UI keys in 8 cultures; 8 new messages in nl + en | PASS |
| IX. Permissive licences | No new dependency | PASS |

## Project Structure

```text
src/SharpCoreLedgerSheets.Domain/Administration/     AdministrationSettings, VatRate, VatScheme
src/SharpCoreLedgerSheets.Domain/Reporting/          VatReturn
src/SharpCoreLedgerSheets.Domain/Services/           VatReturnBuilder (+ IVatReturnBuilder)
src/SharpCoreLedgerSheets.Domain/Accounting/         Period.ForFiscalYear, StarterCharts VAT accounts
src/SharpCoreLedgerSheets.Application/Services/      AdministrationService, VatService (+ interfaces)
src/SharpCoreLedgerSheets.Application/Models/        OpeningBalanceLine, VatInvoiceCommand
src/SharpCoreLedgerSheets.Infrastructure/Storage/    SettingsSchema, SharpCoreDbSettingsRepository
src/SharpCoreLedgerSheets.Web/Pages/                 Settings, Vat (+ views), Journal VAT form
specs/005-correct-books/
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| Opening balances as a journal entry instead of their own table | Keeps one source of truth and avoids touching three statement builders and every existing test | A separate table would need `BalanceSheetBuilder`, `CashFlowBuilder` and `LedgerLoader` to learn about a second input, doubling the paths that can disagree |
| Per-rate VAT accounts in the chart | The return needs per-rate totals, and an account per rate derives them from plain double-entry | Tagging journal lines with a rate would change the schema and the line record for a feature the accounts already express |
| `settings` as key/value rows + JSON for the rates | New settings and new rates need no migration | One column per setting turns every addition into a schema change |
