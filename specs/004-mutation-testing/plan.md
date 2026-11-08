# Implementation Plan: Mutation testing with Stryker.NET

**Branch**: `main` | **Date**: 2026-11-08 | **Spec**: [`spec.md`](spec.md)

**Input**: Feature specification from `specs/004-mutation-testing/spec.md`

## Summary

Pin Stryker.NET as a local tool, give each test project that can host mutation testing a
`stryker-config.json`, prove it works against the real suite (the Microsoft.Testing.Platform runner is the
one detail that decides whether the score is real or 0 %), and put the sweep in a weekly CI workflow so it
never slows down a pull request.

## Technical Context

**Tool**: Stryker.NET 5.0.0, pinned in `.config/dotnet-tools.json` (restored with `dotnet tool restore`)

**Runner**: `"test-runner": "mtp"` - the test projects are xunit.v3 on Microsoft.Testing.Platform. The
default VSTest runner discovered the 156 domain tests and its initial run passed, but its coverage analysis
mapped no test to any mutant, so every mutant was reported as *survived* and the score was 0.00 %. With
`mtp` the mapping works: 27 mutants tested for `Amount.cs`, score 71.43 %.

**Mutation level**: `Standard`, with `ignore-mutations: ["string"]`. The message catalogue makes string
mutants noise: they say nothing about behaviour, and this codebase deliberately centralizes user text.

**Coverage analysis**: `perTest` - each mutant runs only the tests that cover it, which is what keeps a full
domain run at ~22 seconds.

**Thresholds**: high 80, low 60, **break 0**. The score is a diagnostic; the pipeline must not turn red on
day one. The threshold can be raised once the baselines are stable.

**Scope per project**: Domain (everything), Application (everything), Reports (`Services/`, `Models/`,
`Reporting/`), Infrastructure (`Banking/` only - the storage tests open real encrypted databases).

## Constitution Check

| Principle | Gate | Result |
| --- | --- | --- |
| I. A pure domain | No production code changed; the tool is development-only | PASS |
| II. Inward dependencies | No project reference added | PASS |
| III. Tests are the evidence | Mutation testing measures exactly that and exposes the gaps | PASS - baselines recorded |
| IV. Warning-free build | `dotnet build -warnaserror` unchanged and green | PASS |
| V. Small, offline, encrypted | No runtime package, no runtime artifact; reports are git-ignored | PASS |
| VI. Reconcile, never assume | A surviving mutant in the balance sheet or cash flow is now measurable | PASS |
| VII. Cross-platform parity | The tool and configs are platform-neutral; CI runs on ubuntu-latest | PASS |
| VIII. Dutch first, localized everywhere | String mutations are ignored *because* the message catalogue is localized | PASS |
| IX. Permissive licences | Stryker.NET is MIT; it is a dev tool, not a shipped dependency | PASS |

## Project Structure

```text
.config/dotnet-tools.json              # dotnet-stryker 5.0.0 pinned
tests/SharpCoreLedgerSheets.Domain.Tests/stryker-config.json
tests/SharpCoreLedgerSheets.Application.Tests/stryker-config.json
tests/SharpCoreLedgerSheets.Reports.Tests/stryker-config.json
tests/SharpCoreLedgerSheets.Infrastructure.Tests/stryker-config.json
.github/workflows/mutation.yml          # weekly + on demand, three jobs, report artifacts
.gitignore                              # **/StrykerOutput/
README.md, CONTRIBUTING.md              # how to run it, and why "killed" is the only good status
specs/004-mutation-testing/             # this feature
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| A `stryker-config.json` per test project (4 files) | Stryker mutates one project under test per run, and the projects differ in scope and speed | One root config would either mutate the whole solution per project (hours) or force every project to share the storage project's slow settings |
| String mutations ignored | The message catalogue is localized text with no behavioural meaning | Leaving them in would drown the real survivors in dozens of "message text changed" mutants |
| `break = 0` | The baseline is 53-74 %, and a gate at 60 would fail on day one | Gating now would force either weaker thresholds or a big test-writing detour before the tool is even adopted |
