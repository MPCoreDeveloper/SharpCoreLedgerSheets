# Implementation Plan: Localized engine messages, branded reports, Dutch MKB starter chart

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [`spec.md`](spec.md)

**Input**: Feature specification from `specs/003-localized-messages-and-starter-chart/spec.md`

## Summary

Give every user-facing rule message a stable code so the host can translate it, embed the brand mark in the
report layout as a data URI, and ship two starter charts (Dutch MKB and international) that a new
administration can apply in one click. No new dependency, no change to the layering.

## Technical Context

**Language/Version**: unchanged - C# 15 on .NET 11

**Primary Dependencies**: unchanged - SharpCoreDB 2.1.2 is still the only runtime package

**Storage**: `accounts` gains no schema change; the starter chart writes ordinary accounts through the
existing repository

**Testing**: xunit.v3 on Microsoft.Testing.Platform; 348 tests (18 added: 7 domain chart invariants, 4
application chart use cases, 1 report brand mark, 3 message-catalogue tests, 3 more from the earlier phase)

**Target Platform**: unchanged

**Constraints**: the domain must not learn about cultures (codes only); a report must stay one self-contained
file; message catalogues ship for Dutch (default) and English (neutral fallback)

**Scale/Scope**: 53 message codes across 15 files, 2 resource files, 1 report asset, 2 starter charts

## Constitution Check

| Principle | Gate | Result |
| --- | --- | --- |
| I. A pure domain | `MessageCode` is a constants class; no resource or culture type enters the domain | PASS |
| II. Inward dependencies | Web resolves the message; the domain only raises codes | PASS |
| III. Tests are the evidence | Every new behaviour has a test, including a guard over all message codes | PASS - 348 green |
| IV. Warning-free build | `-warnaserror` clean | PASS - 0 warnings |
| V. Small, offline, encrypted | The logo is embedded; no new file is needed to print a report | PASS |
| VI. Reconcile, never assume | Statements untouched | PASS |
| VII. Cross-platform parity | No path or shell dependency added | PASS |
| VIII. Dutch first, localized everywhere | Dutch catalogue complete; English neutral catalogue complete; other cultures fall back | PASS |
| IX. Permissive licences | No new package | PASS |

## Design

### Messages

```text
Domain/Application/Infrastructure raise:
    new DomainException(MessageCode.X, "Nederlandse tekst met {0}", args)
        -> Code = "X", Arguments = [args], Message = Dutch fallback

Web shows (AppPageModel.TryAsync -> ExceptionMessages.Describe):
    localizer["Msg_X"] for the request culture
        -> found      : string.Format(template, args)
        -> not found  : the exception's own message
        -> no code    : the exception's own message
```

The domain keeps speaking Dutch, which is the primary market and keeps every existing domain test honest;
translations live next to the rest of the interface strings in the shared resources.

### Report brand mark

`Branding/sharpcoreledgersheets.png` (64 px) is an embedded resource of the Reports assembly;
`ReportAssets.LogoDataUri` turns it into `data:image/png;base64,…` once, `HtmlReportRenderer` passes it in
`ViewData["ReportLogo"]`, and `_ReportLayout.cshtml` renders it in a `.report-brand` block.

### Starter charts

`StarterCharts` holds the two templates as `StarterAccount` records (number, name, type, flags).
`AccountService.ApplyStarterChartAsync` reads the existing numbers once and adds only the missing ones, so the
operation is idempotent and never touches what the administration already has. The Accounts page posts to
`OnPostStarterAsync` and reports the created count.

## Project Structure

```text
src/SharpCoreLedgerSheets.Domain/
├── Common/MessageCode.cs          # new: the stable message codes
└── Accounting/StarterCharts.cs    # new: the two chart templates

src/SharpCoreLedgerSheets.Reports/
├── Branding/sharpcoreledgersheets.png   # new: embedded brand mark
└── Reporting/ReportAssets.cs            # LogoDataUri

src/SharpCoreLedgerSheets.Web/
├── Localization/ExceptionMessages.cs    # new: code -> localized message
├── Pages/AppPageModel.cs                # renamed from FinPageModel.cs; translates on the way out
├── Pages/Accounts.cshtml(.cs)           # starter-chart picker
└── Resources/SharedResource*.resx       # 53 Msg_ keys (nl + neutral)

specs/003-localized-messages-and-starter-chart/
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| The exception carries both a code and a Dutch message | The domain cannot localize (no dependencies), and the tests assert human-readable domain messages | Codes only would force every domain test to assert an enum value and leave Python-style "code: X" messages to the user |
| Two catalogue files (nl + neutral) | The neutral file is the fallback, so it must hold real English; Dutch must not inherit English | A single `.resx` would make one of the two markets read the other's language |
