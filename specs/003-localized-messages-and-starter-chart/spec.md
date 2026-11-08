# Feature Specification: Localized engine messages, branded reports, Dutch MKB starter chart

**Feature Branch**: `main` (shipped as SharpCoreLedgerSheets 2.1.0)

**Created**: 2026-11-08

**Status**: Implemented

**Input**: The three items that topped the backlog of feature 002: the engine's rule messages were Dutch in
every language, a rendered report carried no brand mark, and a new administration started from an empty
chart of accounts.

## Context

Dutch is the default and the primary market, but an English or German user of 2.0 still saw Dutch rule
messages, because the messages lived as literals in the domain. Reports were branded in text only, and a
fresh installation needed twenty-six accounts typed by hand before it could produce a statement.

This feature closes those three gaps without weakening the principles: the domain stays dependency-free,
reports stay self-contained, and the Dutch experience improves rather than degrades.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A rule message in my own language (Priority: P1)

An English bookkeeper posts an entry that does not balance and reads "Journal entry 'Cash sale' is not in
balance: debit 250 versus credit 200." A Dutch colleague making the same mistake reads "Journaalpost
'Kasverkoop' is niet in balans: debet 250 versus credit 200."

**Why this priority**: A product that translates its interface but not its errors is only half translated,
and the failure is exactly the moment the user needs clarity.

**Independent Test**: raise a coded `DomainException`/`NotFoundException` and assert
`ExceptionMessages.Describe` returns the Dutch text for `nl` and the English text for `en`; assert every
message code resolves in both catalogues.

**Acceptance Scenarios**:

1. **Given** the culture `nl`, **When** an account cannot be found, **Then** the page shows
   "Rekening '1000' is niet gevonden."
2. **Given** the culture `en`, **When** the same failure occurs, **Then** the page shows "Account '1000' was
   not found."
3. **Given** a failure without a code (developer-facing), **When** it surfaces, **Then** the original
   message is shown unchanged.
4. **Given** a culture without a message translation, **When** the failure surfaces, **Then** the Dutch
   message the engine carries is shown instead of a missing-key marker.

---

### User Story 2 - A branded, self-contained report (Priority: P2)

A bookkeeper prints a balance sheet for a client. The document carries the SharpCoreLedgerSheets mark in the
header and still prints identically offline, because the logo travels inside the document.

**Why this priority**: The reports are the product's face towards clients and accountants; a rebrand that
stops at the web header is half a rebrand.

**Independent Test**: render any report and assert the HTML contains a `data:image/png;base64,` logo, the
brand block, and no `src="http…"`.

**Acceptance Scenarios**:

1. **Given** a rendered report, **When** the HTML is inspected, **Then** the logo is embedded as a data URI
   and the document references no network resource.
2. **Given** a report printed to PDF, **Then** the logo appears without an additional file.

---

### User Story 3 - A Dutch MKB chart of accounts in one click (Priority: P3)

Sanne starts a new administration, opens **Accounts**, chooses "Nederlands MKB (RGS)" and clicks "Schema
laden". The chart is ready: cash, bank, receivables, inventory, creditors, VAT payable, equity, revenue and
costs, numbered in the customary Dutch ranges.

**Why this priority**: The statements only work once a chart exists, and typing twenty-six accounts is the
first thing a new user would abandon.

**Independent Test**: apply the chart through `IAccountService.ApplyStarterChartAsync`, assert the accounts
exist with the right flags; apply it twice and assert the second call creates nothing.

**Acceptance Scenarios**:

1. **Given** an empty administration, **When** the Dutch chart is applied, **Then** 26 accounts exist,
   including a cash account (current + cash equivalent) and an inventory account.
2. **Given** an administration that already has account 1000 with its own name, **When** the chart is
   applied, **Then** that account is left untouched and only the missing numbers are created.
3. **Given** the international chart, **When** it is applied, **Then** the numbering matches the Dutch chart
   and the names are English.

---

### Edge Cases

- A message translation with a broken placeholder must never hide the failure: the engine's own message is
  shown instead.
- The starter chart is content: a template that violates an account rule (cash on a non-asset, say) fails in
  a test, not in a user's browser.
- The report logo resource is missing → the report renders without a logo instead of failing.
- The starter chart never renames or deactivates an existing account; it only adds what is missing.

## Requirements *(mandatory)*

### Functional Requirements

| ID | Requirement |
| --- | --- |
| FR-1.1 | A user-facing rule message carries a `MessageCode` plus its values; `DomainException` and `NotFoundException` expose `Code` and `Arguments` |
| FR-1.2 | The host resolves `Msg_<code>` from the shared resources for the request culture |
| FR-1.3 | The Dutch catalogue (`SharedResource.nl.resx`) and the English catalogue (neutral `SharedResource.resx`) contain every message code |
| FR-1.4 | A missing translation or a missing code falls back to the message the exception carries |
| FR-1.5 | Developer-facing failures keep their plain message |
| FR-2.1 | Every report embeds the brand mark as a `data:` URI and stays free of external references |
| FR-2.2 | The report header shows the mark next to the product name |
| FR-3.1 | Two starter charts exist: `DutchMkb` (Dutch names, Dutch numbering, RGS-aligned) and `International` (same numbering, English names) |
| FR-3.2 | `IAccountService.ApplyStarterChartAsync` adds only the accounts that are missing; it is idempotent |
| FR-3.3 | The Accounts page offers the chart picker and reports how many accounts were created, in the user's language |

### Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | No new package dependency |
| NFR-2 | The domain stays free of localization concerns (codes only, no resources) |
| NFR-3 | The build stays warning-free and every suite green |

## Success Criteria *(mandatory)*

- **SC-001**: `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` succeeds with 0 warnings.
- **SC-002**: `dotnet test SharpCoreLedgerSheets.slnx` passes - 348 tests.
- **SC-003**: Every message code resolves in Dutch and English (guard test over the constants).
- **SC-004**: A rendered report contains an embedded logo and no external reference.
- **SC-005**: The starter chart creates 26 accounts once, and nothing on a second application.
- **SC-006**: `GET /Accounts` shows the picker in Dutch and "Starter chart of accounts" with `?culture=en`.

## Assumptions

- Message catalogues are complete for Dutch (default) and English (neutral fallback); the other six cultures
  fall back to English for *messages* until they are translated, while their interface stays translated.
- The starter chart is a starting point, not a commitment: accounts can be renamed and deactivated as usual.
- The brand mark is a 64 px PNG embedded in the Reports assembly; a bigger mark would only inflate every
  report document.

