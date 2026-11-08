# Feature Specification: Correct books from day one (book year, opening balances, VAT)

**Feature Branch**: `main` (shipped as SharpCoreLedgerSheets 2.2.0)

**Created**: 2026-11-08

**Status**: Implemented

**Input**: The first milestone of the product-review roadmap: a Dutch small business must be able to keep
*its own* books in this tool - with a broken book year, with opening balances, and with VAT.

## Context

Until 2.1 the application assumed a calendar year (`Period.ForYear`), had no way to start an administration
mid-year, and left VAT to the user's own arithmetic (the chart reserved a single `Te betalen btw` account).
Those three assumptions make the product unusable for the market it targets: a Dutch freelancer files a
quarterly VAT return, and a shop that takes over a business cannot express its opening position.

This feature closes the three gaps without changing the engine's nature: the ledger stays a pure double-entry
ledger, every VAT booking is a real journal entry, and opening balances are posted once as a reserved entry
instead of becoming a special case in the statements.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - My book year is not the calendar (Priority: P1)

Bram's shop runs a broken book year: February to January. He sets the start month once; the dashboard, the
journal, the reports and the VAT return all follow it.

**Why this priority**: Without it every statement is for the wrong twelve months for a large part of the
Dutch MKB.

**Independent Test**: set the start month to 4 and assert the dashboard renders 2026-04-01 → 2027-03-31.

**Acceptance Scenarios**:

1. **Given** a start month of 2, **When** the dashboard is opened for 2026, **Then** it shows
   1 February 2026 – 31 January 2027.
2. **Given** start month 1, **Then** the year is the ordinary calendar year.
3. **Given** an impossible month (0 or 13), **When** it is saved, **Then** the message says 1..12 is allowed.

---

### User Story 2 - A new book year starts with a position (Priority: P2)

Sanne takes over an existing administration. She enters the balances each account starts with (cash 2 500
debit, equity 2 500 credit) and the books are correct from day one: the balance sheet balances and the cash
flow opens with the right cash.

**Why this priority**: Without opening balances the only way to start is a hand-crafted equity entry, which
is exactly what a small business gets wrong.

**Independent Test**: `ReplaceOpeningBalancesAsync(2026, [1000 debit 2500, 4000 credit 2500])` posts one
entry dated 2025-12-31 with reference `OPENING-2026`; `OpeningBalancesAsync(2026)` returns the two lines.

**Acceptance Scenarios**:

1. **Given** an administration with a chart, **When** the opening balances are saved, **Then** one balanced
   entry dated the day before the book year starts exists and the statements include it.
2. **Given** opening balances from an earlier attempt, **When** they are saved again, **Then** the old entry
   is replaced, not duplicated.
3. **Given** lines that do not balance, **When** they are saved, **Then** the message names both totals and
   nothing is posted.
4. **Given** an empty set, **When** it is saved, **Then** the opening entry is removed.

---

### User Story 3 - Book a sale or a purchase with VAT (Priority: P3)

Sanne sells € 100 of work at 21%. The entry is posted for her: the customer owes € 121, revenue is € 100 and
€ 21 goes to `Te betalen btw 21%`. A purchase does the same in reverse, with `Te vorderen btw` for the input
VAT.

**Why this priority**: This is the daily action of a VAT-registered business, and getting it wrong is what
the tax office notices.

**Independent Test**: `PostSaleAsync(net 100, rate 21)` produces three lines (121 debit, 100 credit, 21
credit); `PostPurchaseAsync(net 100, rate 9)` produces a 100 cost debit, 9 input-VAT debit and 109 supplier
credit.

**Acceptance Scenarios**:

1. **Given** the 21% rate, **When** a sale of 100 is booked, **Then** the VAT is 21.00 on the rate's output
   account and the entry balances.
2. **Given** a net amount of 99.99, **Then** the VAT is rounded half away from zero to 21.00.
3. **Given** the 0% rate, **Then** the entry has two lines and no VAT leg.
4. **Given** an unknown rate, **Then** the message lists the rates that do exist.

---

### User Story 4 - The quarterly VAT return (Priority: P4)

At the end of a quarter Sanne opens **Btw**, picks the quarter and reads per rate what she owes and what she
can reclaim, plus the amount to pay or to receive.

**Why this priority**: It is the reason VAT is worth implementing at all - the numbers a business has to
declare.

**Independent Test**: with a 121 sale and a 121 purchase in Q1 at 21%, the return reports 21 output, 21 input
and a balance of 0; a sale-only quarter reports 21 payable.

**Acceptance Scenarios**:

1. **Given** bookings in Q1 only, **When** Q2 is selected, **Then** the return is zero.
2. **Given** more output than input, **Then** the page shows the amount payable; the reverse shows what can be
   reclaimed.
3. **Given** a rate without accounts in the chart, **Then** the line reports zero instead of failing.

---

### Edge Cases

- A book-year start month outside 1..12 is rejected at the domain boundary.
- Opening balances are validated before anything is written: balanced, non-negative, never both debit and
  credit on one line.
- A failed opening-balance save leaves the previous entry intact (validation happens first).
- A VAT rate whose accounts are missing from the chart reports zero on the return instead of throwing.
- Stored settings are tolerant: an unreadable VAT value falls back to the Dutch scheme, an impossible month to
  January.

## Requirements *(mandatory)*

| ID | Requirement |
| --- | --- |
| FR-1.1 | `AdministrationSettings` holds the company name, the book-year start month, the currency and the VAT scheme |
| FR-1.2 | `Period.ForFiscalYear(year, startMonth)` returns the twelve months from the first of that month |
| FR-1.3 | Settings are persisted (`settings` table) and default to calendar year, euro and the Dutch VAT scheme |
| FR-1.4 | The dashboard, journal, reports and VAT page use the configured book year |
| FR-2.1 | Opening balances are entered per account and per book year and posted as one reserved entry (`OPENING-<year>`) dated the day before the year starts |
| FR-2.2 | Saving again replaces the previous opening entry; an empty set removes it |
| FR-2.3 | Unbalanced, negative or debit-and-credit lines are rejected with a message that names the problem |
| FR-3.1 | The Dutch scheme ships 21%, 9% and 0%; the first two book to their own output and input VAT accounts |
| FR-3.2 | The starter charts carry those accounts (`2100`/`2101` and `1110`/`1111`) |
| FR-3.3 | `VatService` books a sale or purchase from a net amount, a rate and two account numbers, producing one balanced entry |
| FR-3.4 | VAT is rounded to cents, half away from zero; a 0% rate adds no VAT line |
| FR-4.1 | `VatReturnBuilder` derives the return per rate from the VAT account movements in the period |
| FR-4.2 | The VAT page shows the return for a quarter or the whole book year, with payable/reclaimable |
| FR-5.1 | Every new message carries a `MessageCode` and is translated in Dutch and English |
| FR-5.2 | Every new UI string exists in all eight cultures |

### Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | The domain stays free of dependencies and of culture-aware code |
| NFR-2 | No new package reference |
| NFR-3 | The build stays warning-free; the whole suite stays green |
| NFR-4 | Existing 2.1 data files keep working: the new `settings` table is created idempotently |

## Success Criteria *(mandatory)*

- **SC-001**: `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` → 0 warnings, 0 errors.
- **SC-002**: `dotnet test` → **410 passed** (62 new tests: fiscal year, settings, VAT scheme, VAT return, VAT
  booking, opening balances, settings storage).
- **SC-003**: Mutation score of the domain ≥ the 2.1 baseline — **75.12 %** measured (74.49 % before).
- **SC-004**: `/Settings` and `/Vat` render in Dutch and English; the dashboard shows the company name and the
  book-year range.

