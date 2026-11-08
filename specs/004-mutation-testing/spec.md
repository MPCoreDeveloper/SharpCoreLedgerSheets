# Feature Specification: Mutation testing with Stryker.NET

**Feature Branch**: `main` (tooling change in the SharpCoreLedgerSheets 2.1.0 line - no runtime impact)

**Created**: 2026-11-08

**Status**: Implemented

**Input**: Run mutation testing on the repository, so the test suite is measured by what it *catches* and
not only by how much code it touches.

## Context

The suite has 348 tests and they all pass. Passing tests prove that the assertions hold for the cases they
cover - they do not prove that the tests would fail if the production code were subtly wrong. For an
accounting engine that distinction is the whole game: a `<=` where `<` belonged, a dropped
`ConfigureAwait`, a `&&` turned into `||` in the cash-flow classification, and the books still balance in
the test suite while being wrong in reality.

Mutation testing answers that: Stryker.NET changes the production code in small, deliberate ways (a
mutant), runs the tests, and reports a mutant as *killed* when a test fails and *survived* when the suite
does not notice. The percentage killed is the mutation score - a measure of test strength, not of test
count.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A contributor proves a new test is real (Priority: P1)

A contributor adds a rule with tests. Before opening the pull request they run `dotnet stryker` in the test
project and see that every mutant in the new code is killed - or they find the one that survives and add
the assertion it exposes.

**Why this priority**: It is the moment mutation testing pays off - it turns "I wrote tests" into "the
tests would catch this bug".

**Independent Test**: run `dotnet stryker --mutate "Accounting/Amount.cs"` in
`tests/SharpCoreLedgerSheets.Domain.Tests` and see a real score (not 0%, not an error), with the surviving
mutants listed per line.

**Acceptance Scenarios**:

1. **Given** a restored repository, **When** `dotnet tool restore` runs, **Then** the pinned Stryker version
   is available without a global install.
2. **Given** the domain test project, **When** `dotnet stryker` runs, **Then** it discovers the xunit.v3
   tests on Microsoft.Testing.Platform and reports a mutation score.
3. **Given** a report in `StrykerOutput/<timestamp>/reports/`, **When** the HTML is opened, **Then** every
   surviving mutant shows its file, line and the original/mutated code.

---

### User Story 2 - The team sees the score without blocking the build (Priority: P2)

Each week CI mutates the domain, application and reports projects and uploads the reports. A regression in
test strength becomes visible as a trend instead of being discovered by an escaped bug.

**Why this priority**: Mutation runs take minutes, so they must not sit on the pull-request path; a weekly
sweep plus on-demand runs keeps the signal without the cost.

**Independent Test**: trigger `Mutation testing` from the Actions tab and confirm three jobs and three
uploaded report artifacts.

**Acceptance Scenarios**:

1. **Given** the workflow, **When** it runs, **Then** each project gets its own job with its own report
   artifact, and one failing project does not cancel the others.
2. **Given** a low score, **When** the run finishes, **Then** the build stays green (`break = 0`) but the
   score and the survivors are in the log and the report.

## Requirements *(mandatory)*

| ID | Requirement |
| --- | --- |
| FR-1 | Stryker.NET is pinned in `.config/dotnet-tools.json` and restored with `dotnet tool restore` |
| FR-2 | Every test project that can host mutation testing has a `stryker-config.json`: Domain, Application, Reports and Infrastructure |
| FR-3 | The configuration uses the Microsoft.Testing.Platform runner, because the projects are xunit.v3 on MTP |
| FR-4 | Reports are written as HTML and JSON with a per-project file name, into `StrykerOutput/` (git-ignored) |
| FR-5 | Thresholds: high 80, low 60, break 0 - the score is a diagnostic, not a gate |
| FR-6 | CI runs the three main projects weekly and on demand, uploading the reports as artifacts |
| FR-7 | `README.md` and `CONTRIBUTING.md` explain how to run it and what a surviving mutant means |
| FR-8 | `Infrastructure.Tests` mutates only `Banking/` (the statement readers), because the storage tests spin up real encrypted databases |

### Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | No runtime dependency: the tool is a development tool, not a package reference |
| NFR-2 | The tool is free and MIT-licensed (Stryker.NET) |
| NFR-3 | A full domain run finishes in well under a minute on a laptop, so contributors actually use it |
| NFR-4 | Mutation output never enters version control |

## Baseline (measured 2026-11-08, this machine)

| Project | Mutants created | Tested | Mutation score | Run time |
| --- | --- | --- | --- | --- |
| Domain | 646 | 328 | **74.49 %** | 22 s |
| Application | 204 | 120 | **52.67 %** | 15 s |
| Reports | 237 | 105 | **54.46 %** | 16 s |
| Infrastructure (Banking only) | - | - | not yet run | opt-in |

The gap between Domain (74 %) and Application/Reports (~53 %) is the interesting finding: the domain tests
were written against hand-computed accounting scenarios, while the use-case and rendering tests mostly
assert the happy path. That is the work the score now makes visible.

## Success Criteria *(mandatory)*

- **SC-001**: `dotnet tool restore` + `dotnet stryker` produce a non-zero, plausible score for the domain
  project (the 0 % "everything survived" trap does not occur).
- **SC-002**: The three main projects are covered by a weekly, report-uploading CI job.
- **SC-003**: The build and the pull-request pipeline keep their current run time; mutation testing lives
  outside them.
- **SC-004**: The baselines above are recorded, so future improvements are measurable.
