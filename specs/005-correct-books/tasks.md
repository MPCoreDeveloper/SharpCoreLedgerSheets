# Tasks: Correct books from day one

**Input**: `specs/005-correct-books/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`

All tasks are complete (`[x]`); each names the artifact that proves it.

## Phase 1: Book year and settings (US1)

- [x] T001 `Period.ForFiscalYear(year, startMonth)` with 1..12 validation and calendar fallback (`Domain/Accounting/Period.cs`)
- [x] T002 `AdministrationSettings` with company name, start month, currency and VAT scheme + `PeriodFor` (`Domain/Administration/AdministrationSettings.cs`)
- [x] T003 `VatRate` and `VatScheme` with the Dutch defaults (21 -> 2100/1110, 9 -> 2101/1111, 0 -> none) and `Require`/`Find` (`Domain/Administration/VatScheme.cs`)
- [x] T004 [P] Message codes for the new rules (`FiscalYearMonthInvalid`, `CompanyNameRequired`, `VatRateDuplicate`, `VatRateUnknown`, `VatSchemeEmpty`, `VatAccountsMissing`, `OpeningBalanceNotBalanced`, `AmountMustBePositive`) + nl/en catalogue entries
- [x] T005 `ISettingsRepository` port + `SettingsSchema` (`settings` table) + `SharpCoreDbSettingsRepository` with tolerant parsing; wired into `SharpCoreDbFactory` and the DI extensions
- [x] T006 [P] Domain tests: `FiscalYearTests` (calendar, broken year, December start, leap-year boundary, invalid months), `AdministrationSettingsTests`, `VatSchemeTests`
- [x] T007 [P] Infrastructure tests: `SharpCoreDbSettingsRepositoryTests` (defaults, round-trip through a reopen, replace, empty VAT scheme)

## Phase 2: Opening balances (US2)

- [x] T008 `OpeningBalanceLine` model and `IAdministrationService` (settings + `OpeningBalancesAsync` + `ReplaceOpeningBalancesAsync`)
- [x] T009 `AdministrationService`: validate (balanced, non-negative, debit-xor-credit), remove the previous `OPENING-<year>` entry, post one balanced entry dated the day before the book year
- [x] T010 [P] `AdministrationServiceTests`: dated correctly, broken book year honoured, read-back, replace instead of duplicate, empty set clears, three rejection paths
- [x] T011 Settings page with the company/book-year form and the per-account opening-balance table (`Pages/Settings.cshtml(.cs)`)

## Phase 3: VAT (US3, US4)

- [x] T012 Per-rate VAT accounts in both starter charts (`1110`, `1111`, `2100`, `2101`)
- [x] T013 `VatInvoiceCommand`, `IVatService` and `VatService` (sale/purchase, rounding half away from zero, 0% without a VAT leg, unknown rate and empty scheme rejected)
- [x] T014 [P] `VatServiceTests`: sale, purchase, zero rate, rounding, unknown rate, empty scheme, non-positive net, null command
- [x] T015 `VatReturn` + `VatReturnBuilder` (+ interface) reading the return from the VAT account movements
- [x] T016 [P] `VatReturnBuilderTests`: per-rate output/input, payable, reclaimable, period isolation, rate without accounts, empty scheme
- [x] T017 VAT page with year and quarter selection, per-rate table and payable/reclaimable badge (`Pages/Vat.cshtml(.cs)`)
- [x] T018 VAT booking form on the journal page (`Pages/Journal.cshtml(.cs)` - `OnPostVatAsync`)

## Phase 4: Wiring, localization and delivery

- [x] T019 Dashboard, journal, reports and VAT page use the configured book year; the dashboard shows the company and the period
- [x] T020 [P] 22 new UI keys in all eight cultures; the keys added to `LocalizationTests`
- [x] T021 Navigation entries for **Btw** and **Instellingen**
- [x] T022 Version 2.2.0; README sections for book year, opening balances and VAT
- [x] T023 Record this feature (`spec.md`, `plan.md`, `tasks.md`, `research.md`) and mark VAT as shipped in the 002/003 backlogs
- [x] T024 Verify: build `-warnaserror`, full suite, mutation run, live smoke test of `/`, `/Settings`, `/Vat` (nl + en)

---

## Verification Log

| Gate | Result |
| --- | --- |
| `dotnet build SharpCoreLedgerSheets.slnx -c Release -warnaserror` | **0 warnings / 0 errors** |
| `dotnet test SharpCoreLedgerSheets.slnx -c Release` | **410 passed**, 0 failed (188 domain, 86 application, 83 infrastructure, 27 reports, 26 architecture) |
| `dotnet stryker` (domain) | **75.12 %** - 766 mutants created, 389 tested (baseline 74.49 % on 646/328) |
| Smoke test | `/` → 200 with the company name and *Boekjaar*; `/Settings` → 200 with *Start boekjaar* + *Beginsaldi*; `/Vat` → 200 with *Af te dragen btw* + *Kwartaal*, and *Output VAT* with `?culture=en` |

## Notes

- Opening balances are journal data (a reserved `OPENING-<year>` entry), not a second source of truth - see
  `plan.md` and `research.md` for the trade-off.
- The VAT return deliberately reports *per rate* VAT rather than the turnover lines (1a/1b) of the Dutch
  form: turnover per rate needs invoice-level data, which is the invoice milestone.
- Infrastructure mutation testing stays opt-in; the new storage code is covered by the settings round-trip
  tests through a real encrypted database.
