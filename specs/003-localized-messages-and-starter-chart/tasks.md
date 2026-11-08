# Tasks: Localized engine messages, branded reports, Dutch MKB starter chart

**Input**: `specs/003-localized-messages-and-starter-chart/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`

**Organization**: One phase per item, all complete (`[x]`), each naming the artifact that proves it.

## Phase 1: Localized rule messages (US1)

- [x] T001 Add `MessageCode` with the 53 stable codes in `src/SharpCoreLedgerSheets.Domain/Common/MessageCode.cs`
- [x] T002 Give `DomainException` a `Code`, `Arguments` and a `(code, format, args)` constructor that renders the Dutch fallback in `src/SharpCoreLedgerSheets.Domain/Common/DomainException.cs`
- [x] T003 Give `NotFoundException` the same shape in `src/SharpCoreLedgerSheets.Application/Exceptions/NotFoundException.cs`
- [x] T004 [P] Rewrite the domain rule messages (accounts, amounts, periods, entries, lines, movements, rules, proposals) to carry a code
- [x] T005 [P] Rewrite the application messages (accounts, entries, bank statements) to carry a code
- [x] T006 [P] Rewrite the importer messages (CAMT.053, CSV) and the bank-import page message to carry a code
- [x] T007 Add `ExceptionMessages.Describe` in `src/SharpCoreLedgerSheets.Web/Localization/ExceptionMessages.cs` (resource lookup, `string.Format`, safe fallbacks)
- [x] T008 Translate on the way out in `Pages/AppPageModel.cs` (renamed from `FinPageModel.cs`) so every page benefits without changes
- [x] T009 [P] Add the 53 English `Msg_*` keys to `Resources/SharedResource.resx` (neutral fallback)
- [x] T010 [P] Add the 53 Dutch `Msg_*` keys to `Resources/SharedResource.nl.resx` (the default culture must not fall through to English)
- [x] T011 Guard it: `EveryMessageCode_HasATranslationForDutchAndEnglish`, `ExceptionMessages_TranslatesADomainRule`, `ExceptionMessages_FallsBackWhenThereIsNothingToTranslate` in `tests/SharpCoreLedgerSheets.ArchitectureTests/LocalizationTests.cs`

## Phase 2: Branded reports (US2)

- [x] T012 Add the 64 px brand mark as an embedded resource: `src/SharpCoreLedgerSheets.Reports/Branding/sharpcoreledgersheets.png` + `<EmbeddedResource>` in the Reports csproj
- [x] T013 [P] Expose it as a lazy `data:` URI in `Reporting/ReportAssets.cs` and pass it as `ViewData["ReportLogo"]` from `Services/HtmlReportRenderer.cs`
- [x] T014 Render the mark in `Templates/Shared/_ReportLayout.cshtml` (`.report-brand`) with the matching CSS in `ReportAssets.Css`
- [x] T015 Prove self-containment: `RenderHtmlAsync_EmbedsTheBrandMarkAsDataUri` in `tests/SharpCoreLedgerSheets.Reports.Tests/HtmlReportRendererTests.cs`

## Phase 3: Starter charts (US3)

- [x] T016 Add `StarterChart`, `StarterAccount` and `StarterCharts` (Dutch MKB + international, 26 accounts each, same numbering) in `src/SharpCoreLedgerSheets.Domain/Accounting/StarterCharts.cs`
- [x] T017 Add `IAccountService.ApplyStarterChartAsync` and implement it idempotently in `Services/AccountService.cs`
- [x] T018 Add the picker and the created-count message to `Pages/Accounts.cshtml(.cs)` (`OnPostStarterAsync`)
- [x] T019 [P] Add the six UI keys to all eight cultures (`StarterChart`, `StarterChartHint`, `StarterChartLoad`, `StarterChartCreated`, `StarterChartDutch`, `StarterChartInternational`)
- [x] T020 [P] Domain invariants in `tests/SharpCoreLedgerSheets.Domain.Tests/Accounting/StarterChartTests.cs` (unique numbers, every type present, cash present, the domain accepts every template, both charts share the numbering)
- [x] T021 [P] Use cases in `tests/SharpCoreLedgerSheets.Application.Tests/Services/AccountServiceTests.cs` (created, idempotent, existing accounts untouched, international names)
- [x] T022 Style the `.melding`/`.velden`/`.veld` classes that the pages already used but the stylesheet did not define, in `wwwroot/site.css`

## Phase 4: Delivery

- [x] T023 Update `README.md` (localized messages, report logo, starter chart, 348 tests), `CONTRIBUTING.md` (message-code rule) and the constitution (1.2.0)
- [x] T024 Update the backlog in `specs/002-rebrand-and-english-schema/research.md` to point at this feature
- [x] T025 Verify: build `-warnaserror` → 0 warnings; `dotnet test` → 348 passed; live smoke test on `/Accounts` (nl + en) and `/Report`

---

## Verification Log

| Gate | Result |
| --- | --- |
| `dotnet build SharpCoreLedgerSheets.slnx -c Release -warnaserror` | **0 warnings / 0 errors** |
| `dotnet test SharpCoreLedgerSheets.slnx -c Release` | **348 passed**, 0 failed (156 domain, 60 application, 79 infrastructure, 27 reports, 26 architecture) |
| Smoke test | `/Accounts` → 200 and "Startrekeningschema" + "Nederlands MKB (RGS)"; `/Accounts?culture=en` → 200 and "Starter chart of accounts"; `/Report` → 200 |

## Notes

- The message catalogue is complete for Dutch and English; the other six cultures show English *messages*
  until they are translated, while their interface stays translated. That is the documented trade-off.
- A new user-facing message means: a `MessageCode` constant, a Dutch `Msg_*` value and an English `Msg_*`
  value - the guard test fails until both exist.
