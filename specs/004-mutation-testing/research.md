# Research & Decisions: Mutation testing

**Input**: `spec.md` and `plan.md` for `specs/004-mutation-testing/`

## Decisions

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| **Stryker.NET** | Manual mutation, or line coverage as a proxy | Coverage says a line ran; Stryker says whether a test would notice a change. It is the established .NET tool, MIT-licensed, and pins cleanly in a tool manifest |
| The tool is pinned in `.config/dotnet-tools.json` | A global install, or a package reference in a project | `dotnet tool restore` makes the version part of the repository; a package reference would ship the tool to consumers |
| **One `stryker-config.json` per test project** | A single root configuration | Stryker mutates one project under test per run, and the four projects differ wildly in scope and speed (22 s vs tens of minutes). Per-project configs let CI run three cheap jobs and leave the slow one opt-in |
| **`"test-runner": "mtp"`** | The default VSTest runner | With VSTest the tool discovered the tests and the initial run passed, but the coverage analysis mapped no test to a mutant, so all 28 mutants of `Amount.cs` were reported as *survived* (score 0.00 %). The projects run xunit.v3 on Microsoft.Testing.Platform; with `mtp` the mapping works and the score is 71.43 % |
| `coverage-analysis: perTest` | `all` (every test per mutant) | Per-test coverage is what keeps a full domain sweep at 22 seconds; without it every mutant re-runs all 156 tests |
| `ignore-mutations: ["string"]` | Mutating message text too | The message catalogue makes string mutants meaningless noise (they cannot be killed without asserting on prose), and this codebase centralizes user text on purpose |
| `thresholds.break = 0` | Gate the build at 60 % | The honest baseline is 52-74 %; a gate on day one would force fake scores or a large test detour before the tool proves its worth. The threshold is the goal, not the entry fee |
| Baselines measured and recorded | "Run it and see" | A score without a recorded baseline cannot show improvement. The three numbers (74.49 / 52.67 / 54.46) are the reference for future work |
| Weekly schedule + manual dispatch | Running on every pull request | 22-45 s per project is tolerable, but the value is in the trend, not in blocking a change; the pull-request path stays fast |
| `Infrastructure/` mutated only under `Banking/` | Mutating the whole infrastructure project | Every storage test opens a real encrypted database; the statement readers are the part where pure logic lives and where mutants are cheap to test |

## Findings

1. **A green mutation table can be a lie.** The VSTest run produced 39 mutants, 28 survived, 11 ignored,
   zero killed, score 0.00 %, and no `coveredBy` on any mutant - and the process still exited successfully.
   The tell is `coveredBy` being empty and `killedBy` never appearing; always sanity-check a first run
   instead of trusting the number.
2. **The domain is the strongest part of the suite.** 74.49 % with hand-computed accounting scenarios.
   The survivors cluster in flag validation and the ratio/statement builders' edge branches.
3. **The use cases and the report engine are the weak spots** (52.67 % and 54.46 %). Their tests assert the
   happy path; the mutants that survive are guard clauses and exporter formatting branches.
4. **Compile-error mutants are normal** (13 in the domain): Stryker reports them and excludes them from the
   score. They are not a defect.
5. **The tool costs nothing at runtime.** No package reference, no build change, and `StrykerOutput/` is
   git-ignored; the `dotnet build -warnaserror` and `dotnet test` paths are untouched.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Someone "fixes" a 0 % score by adding `[ExcludeFromCodeCoverage]` or ignoring mutations | `CONTRIBUTING.md` rule 6: kill the mutant or ignore the mutation with a reason; never lower the threshold |
| The tool pin drifts or breaks on a new SDK | The version lives in the manifest (Dependabot's NuGet ecosystem updates it), and CI runs the sweep weekly - so a break shows up within a week, not at the next release |
| Mutation runs creep into the pull-request pipeline | The workflow is schedule/dispatch only; the constitution lists mutation testing as a gate you run on the project you touched, not on every push |
| A future config edit drops `test-runner: mtp` | The 0 % signature is documented in this file and in `tasks.md` T008/T009 - and a 0 % score is impossible to miss |

## Backlog (post-2.1, ordered)

1. **Close the Application and Reports gap** - assert the guard clauses and exporter branches the survivors
   point at; target 70 % for both.
2. **Raise `break`** to the recorded baseline per project once the numbers are stable (Domain first), so the
   pipeline protects the achieved strength without chasing the aspirational high threshold.
3. **Infrastructure (Banking) baseline** - run it once and record it; then decide whether it belongs in CI.
4. **Stryker dashboard** - the report already carries `project-info`; publishing to
   dashboard.stryker-mutator.io would give the score a public history.
