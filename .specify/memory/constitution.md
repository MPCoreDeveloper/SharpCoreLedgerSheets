# SharpCoreLedgerSheets Constitution

SharpCoreLedgerSheets is an embedded double-entry bookkeeping engine and web application: one encrypted file,
one ledger, and the three core financial statements plus the key ratios, derived from that ledger.
These principles are binding for every change. Where they conflict with convenience, they win.

## Core Principles

### I. A Pure Domain (NON-NEGOTIABLE)

`SharpCoreLedgerSheets.Domain` has **zero** package references and no framework types. The bookkeeping rules,
the statements and the ratios are pure, deterministic C# that can be reasoned about and tested
without a database, a host or a container. Anything that needs I/O, storage, rendering or HTTP
belongs in a layer that depends on the domain, never the other way round.

### II. Dependencies Point Inwards

`Web -> Reports/Infrastructure -> Application -> Domain`, and never backwards. `Application` owns
the ports (`IAccountRepository`, `IJournalEntryRepository`, ...); `Infrastructure` implements them;
`Reports` renders; `Web` composes. This is tested, not merely intended: `LayerDependenciesTests`
fails the build when a layer reaches outwards or when the domain picks up a framework dependency.

### III. Tests Are the Evidence

Every behaviour change lands with a test that would fail without it, and every requirement names the
artifact that proves it. Non-negotiables that follow from this:

- Domain rules are tested with **hand-computed** expectations, not by recomputing them with the code
  under test.
- Storage behaviour is tested **round-trip, then reopen**: assert after closing and reopening the
  database, because persistence bugs hide behind a warm cache.
- Rendering is tested by **running the real Razor pipeline**; mocking the view engine proves nothing.
- Assertions come from `xunit.v3` itself (`Assert.Equal`, `Assert.Contains`, `Assert.Throws`). No
  FluentAssertions, Shouldly or comparable package - the expected value comes first.

### IV. The Build Is Warning-Free (NON-NEGOTIABLE)

The Microsoft analyzers run at the latest recommended level and SonarAnalyzer runs alongside them.
CI builds with `-warnaserror`. A change that introduces a warning is not mergeable: fix the cause,
do not suppress the rule. `NoWarn` is reserved for documented false positives (currently two Razor
template suggestions, `S3358` and `S6966`).

### V. Small, Offline, Encrypted

The product must stay small and self-contained:

- Storage is one encrypted SharpCoreDB `.scdb` file; there is no database server and no cloud.
- No Node.js, no front-end build step, no design-time tooling, no headless browser.
- Reports are self-contained HTML (embedded styling, no JavaScript, no CDN and no external URL) plus
  CSV; PDF comes from the browser's print dialog.
- Expected failures surface as readable messages (`DomainException`, `NotFoundException`), never as a
  stack trace.

### VI. Reconcile, Never Assume

The statements are three views of the same journal entries, so they must reconcile by construction and
say so: `BalanceSheet.IsBalanced` (assets = liabilities + equity + result) and
`CashFlowStatement.IsBalanced` (opening cash + net movement = closing cash) are asserted in code and
covered by tests. A statement that cannot be reconciled is a defect, not a presentation detail.

### VII. Cross-Platform Parity

The same commands work on Linux, Windows and macOS: `dotnet restore`, `dotnet build`, `dotnet test`,
`dotnet pack`. CI runs restore -> build (warnings as errors) -> test -> pack on all three, and a
dependency audit job flags known-vulnerable packages.

### VIII. Dutch First, Localized Everywhere

The target market is the Dutch MKB, so **Dutch is the default culture**: a Dutch entrepreneur gets a
Dutch interface, Dutch number and date formatting and Dutch report labels without configuring
anything. English is the neutral fallback - the neutral `SharedResource.resx` holds the English text -
and German, French, Spanish, Italian, Portuguese and Polish ship alongside it. Every UI and report key
resolves in every supported culture, guarded by `LocalizationTests`, which fails loudly, and report
templates never hard-code a word: all user-visible text comes from `IReportLabels`. A rule a user can
trigger carries a `MessageCode`, and the host translates it from the `Msg_<code>` catalogue - so an error
speaks the language of the interface, and the Dutch text the engine carries is only the fallback. The code,
the API and the schema are English, so a contributor never needs Dutch to work on the engine.

### IX. Permissive Licences Only

Everything the repository depends on must be MIT or Apache-2.0 so the suite is usable in any context.
One documented exception: `SonarAnalyzer.CSharp` is source-available (Sonar Source-Available License
v1.0). It is accepted because it is build-time only (`PrivateAssets=all`), is never shipped, and its
licence only forbids building a competing SonarQube product. Any new exception requires the same
written justification next to the reference.

## Additional Constraints

| Area | Constraint |
| --- | --- |
| Language | .NET 11 / C# 15, nullable enabled, implicit usings, file-scoped namespaces |
| SDK | Pinned in `global.json` (`rollForward: latestFeature`, prereleases allowed while .NET 11 is in preview) |
| Build settings | Shared in `Directory.Build.props`: analyzers, packaging metadata, MIT licence expression |
| Storage engine | SharpCoreDB - embedded, encrypted, single file; repositories serialize writes on one handle |
| Money | `Amount` value object on `decimal`; amounts round to the currency's decimals and mixed-currency arithmetic throws |
| Dates | `DateOnly` in code, ISO-8601 `yyyy-MM-dd` `TEXT` in storage so they sort chronologically |
| Naming | English type, member, table and column names - the schema reads `accounts`, `entries`, `entry_lines`, `banktxns`, `matchrules` |
| Schema names | Short by necessity: SharpCoreDB caps metadata block names at 31 characters, which is why the tables are `banktxns`/`matchrules` rather than long descriptive names |
| Localization | Dutch is the default culture (Dutch MKB); the neutral `SharedResource.resx` is the English fallback; user-visible text never lives in code |

## Development Workflow & Quality Gates

Design work follows Spec Kit, not an ad-hoc process:

- `.specify/memory/constitution.md` (this file) - the principles above.
- `specs/<feature>/spec.md` - user stories, functional requirements and success criteria.
- `specs/<feature>/plan.md` - technical design and constitution check.
- `specs/<feature>/tasks.md` - ordered, evidence-backed tasks.
- `specs/<feature>/research.md` - decisions, alternatives rejected, risks and lessons learned.

Slash commands (`/speckit-constitution`, `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`,
`/speckit-implement`, plus `/speckit-clarify`, `/speckit-analyze` and `/speckit-checklist`) drive
that workflow with the coding agent.

Quality gates before a pull request is mergeable:

1. `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` - zero warnings.
2. `dotnet test SharpCoreLedgerSheets.slnx` - green, including the architecture and localization tests.
3. `dotnet list SharpCoreLedgerSheets.slnx package --vulnerable --include-transitive` - no known vulnerabilities.
4. The behaviour change is described, the reasoning is explained, and the spec artifacts are updated
   when a decision changes.

Commits stay small and explain *why*. Test dependencies stay free (see principle IX).

## Governance

This constitution supersedes other practices and habits. Amendments are made by pull request that
states the change, the reason and the migration impact; the version line below is bumped with
MAJOR for a principle being removed or redefined, MINOR for a principle or section being added, and
PATCH for clarifications. Every review verifies compliance with the principles above, and complexity
that violates a principle must be justified in the feature's `plan.md` (Complexity Tracking) rather
than silently accepted.

**Version**: 1.2.0 | **Ratified**: 2026-11-08 | **Last Amended**: 2026-11-08
