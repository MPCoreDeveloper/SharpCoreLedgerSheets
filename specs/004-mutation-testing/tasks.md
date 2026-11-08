# Tasks: Mutation testing with Stryker.NET

**Input**: `specs/004-mutation-testing/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`

**Organization**: Four phases, all complete (`[x]`), each naming what proves it.

## Phase 1: Tool and configuration

- [x] T001 Create the tool manifest and pin the tool: `.config/dotnet-tools.json` with `dotnet-stryker` 5.0.0 (`dotnet new tool-manifest` + `dotnet tool install dotnet-stryker`)
- [x] T002 Generate a schema-correct starting point with `dotnet stryker init` in `tests/SharpCoreLedgerSheets.Domain.Tests` (then strip it to the options this repo actually sets)
- [x] T003 [P] `tests/SharpCoreLedgerSheets.Domain.Tests/stryker-config.json` - whole domain, MTP runner, thresholds 80/60/0, HTML+JSON+cleartext reports
- [x] T004 [P] `tests/SharpCoreLedgerSheets.Application.Tests/stryker-config.json` - `project: SharpCoreLedgerSheets.Application.csproj`
- [x] T005 [P] `tests/SharpCoreLedgerSheets.Reports.Tests/stryker-config.json` - scoped to `Services/`, `Models/`, `Reporting/`
- [x] T006 [P] `tests/SharpCoreLedgerSheets.Infrastructure.Tests/stryker-config.json` - scoped to `Banking/`, concurrency 4 (the storage tests open real databases)
- [x] T007 [P] Ignore `**/StrykerOutput/` and `stryker.log` in `.gitignore`

## Phase 2: Prove the score is real

- [x] T008 Scoped run on one file (`--mutate "Accounting/Amount.cs"`) - found the trap: with the default VSTest runner all 28 mutants "survived" (score 0.00 %) because the coverage analysis mapped no tests
- [x] T009 Switch the runner: `--test-runner mtp` → 27 mutants tested, score 71.43 %, `killedBy` populated
- [x] T010 Put `"test-runner": "mtp"` in all four configs and confirm a run without flags uses it
- [x] T011 Baseline runs: Domain 646 created / 328 tested / **74.49 %** in 22 s; Application 204 / 120 / **52.67 %** in 15 s; Reports 237 / 105 / **54.46 %** in 16 s

## Phase 3: Continuous integration

- [x] T012 `.github/workflows/mutation.yml`: weekly cron (Mondays 03:00 UTC) + `workflow_dispatch`, matrix over domain/application/reports, `fail-fast: false`, 45-minute job timeout
- [x] T013 Steps: `dotnet tool restore` → `dotnet restore SharpCoreLedgerSheets.slnx` → `dotnet stryker --config-file stryker-config.json` in the project directory
- [x] T014 Upload `StrykerOutput/**/reports/*` as `mutation-report-<scope>` with `if: always()`

## Phase 4: Documentation and governance

- [x] T015 `README.md`: a "Mutation testing" section - how to run it, what each project mutates, the three deliberate configuration choices, and the score-not-gate stance
- [x] T016 `CONTRIBUTING.md`: ground rule 6 - a surviving mutant is a test gap; kill it or ignore the mutation with a reason, never lower the threshold
- [x] T017 Constitution 1.3.0: mutation testing is quality gate 4, watched week-to-week through this feature's baselines
- [x] T018 Record this feature (`spec.md`, `plan.md`, `tasks.md`, `research.md`) including the baselines

---

## Verification Log

| Gate | Result |
| --- | --- |
| `dotnet tool restore` | restores `dotnet-stryker 5.0.0` from the manifest |
| `dotnet stryker` in `Domain.Tests` | **74.49 %** - 328 of 646 mutants tested, the rest filtered (string mutations, block-covered, compile errors, no coverage) |
| `dotnet stryker` in `Application.Tests` | **52.67 %** - 120 mutants tested |
| `dotnet stryker` in `Reports.Tests` | **54.46 %** - 105 mutants tested |
| `dotnet build -warnaserror` / `dotnet test` | unchanged: 0 warnings, 348 tests green (the tool adds no build or test surface) |
| Baseline run times | 22 s / 15 s / 16 s - fast enough to run before a pull request |

## Notes

- The single most important setting is `test-runner: mtp`: with VSTest the tool *runs*, reports a table, and
  lies (everything survives). That failure mode is recorded here because it is silent.
- `Infrastructure.Tests` is deliberately outside CI: mutating code where every test opens an encrypted
  database file would take tens of minutes. Run it on demand, `Banking/` only.
- The next win is not a higher threshold but the Application/Reports gap: most survivors there are
  unasserted branches in the use cases and the report exporter.
