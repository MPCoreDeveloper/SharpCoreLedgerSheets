# Research & Decisions: Correct books from day one

**Input**: `spec.md` and `plan.md` for `specs/005-correct-books/`

## Decisions

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| Settings are an aggregate persisted as key/value rows | A column per setting, or settings in `appsettings.json` | The book year and the company belong to the *administration* (they travel with the data file), not to the host. Key/value rows mean a new setting never needs a migration. |
| The fiscal year is stored as a **start month** (day 1) | A full start date | A book year that starts mid-month is not a thing in practice, and a month keeps validation honest (1..12) with no "31 February" edge cases |
| Opening balances are **one reserved journal entry** (`OPENING-<year>`, dated the day before the year) | A separate `opening_balances` table read by the statement builders | The entry keeps a single source of truth: the statements already sum the ledger, the cash flow opens correctly with no extra code, and the position is visible and auditable in the journal. A table would have forced `BalanceSheetBuilder`, `CashFlowBuilder` and `LedgerLoader` to accept a second input - more paths that can disagree |
| Replacing opening balances deletes and re-posts the entry | Updating the entry in place | An entry is immutable once posted (principle: correcting entries over edits); delete-and-post in one operation is the same semantics with less code |
| VAT is booked as **ordinary ledger lines** to per-rate VAT accounts | Tagging journal lines with a VAT rate (schema change) | The accounts express the split without touching the schema, the entry stays readable by an accountant, and the return derives per-rate totals from plain movements |
| The scheme ships 21% / 9% / 0% with `2100`/`2101` output and `1110`/`1111` input accounts | One generic `Te betalen btw` account | The Dutch return separates by rate; one account would force re-deriving the split from turnover, which needs invoice data |
| VAT rounds **half away from zero** to cents | Banker's rounding | It is what an invoice shows and what the tax office expects; the rule is in one place (`VatService.VatOn`) and covered by a theory |
| The VAT return reports per-rate output/input and the payable/reclaimable amount | The full Dutch form including turnover lines 1a/1b | Turnover per rate cannot be derived from VAT accounts alone; that needs invoice-level data (the invoice milestone). What ships is 100 % derivable and is the number a business pays |
| Reading settings tolerates garbage (unknown month, unreadable VAT JSON) | Failing on corrupt settings | An administration must open; a corrupt setting must degrade to a sane default, not lock the user out of their books |
| The `settings` table is created idempotently on open | A migration runner | Same pattern as the existing schema: additive, `IF NOT EXISTS`, works on 2.1 files |

## Findings

1. **A broken book year touches more than the period.** The report metadata carried no `Administration`
   value, so reports showed no company name; wiring the settings in fixed that too.
2. **The starter chart was short of VAT accounts.** It had a single `Te betalen btw`; without per-rate
   accounts the return cannot separate 21 % from 9 %. Both charts now carry four VAT accounts, and the
   numbering test keeps them in step.
3. **`InlineData` carries doubles, not decimals.** The first version of the rounding theory failed on
   `0.03` because the attribute stores `0.0299999…`; passing cents as integers makes the expectations exact.
4. **Sonar insists on cancellation tokens in async test helpers** (S8949) - the seed helpers pass `Ct`.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| A user deletes the `OPENING-<year>` entry in the journal, silently losing the opening position | It is visible in the journal with its reserved reference, and re-saving the settings re-posts it. A guard (refuse deletion of opening entries) is a backlog item |
| Someone books VAT through the plain two-line form instead of the VAT form | The two-line form still works (it is a ledger), but the VAT return ignores it because no VAT account was touched - the VAT page shows zero. Documentation and the VAT form's presence steer to the right path |
| Rounding differences per line vs. per invoice | The VAT is computed per booked line with a single documented rule; a future invoice entity will aggregate first and round once |
| Settings edited by hand break a rate | Unreadable JSON falls back to the Dutch scheme; a duplicate or unknown rate is rejected at construction with a message that lists the valid codes |

## Backlog (post-2.2, ordered)

1. **Turnover lines on the VAT return** (1a/1b/1e) once invoices exist, so the form can be filed without
   recomputing anything.
2. **VAT corrections** - a credit note flow (reverse sale/purchase) with its own form.
3. **Protect the opening entry** - refuse to delete or edit a journal entry whose reference starts with
   `OPENING-`.
4. **Recurring entries** - the next daily-use gap: rent, subscriptions and salaries booked once and repeated
   monthly.
