# Research & Decisions: SharpCoreLedgerSheets 1.0

**Input**: `spec.md` and `plan.md` for `specs/001-sharpcoreledgersheets/`

**Purpose**: why the product is built the way it is - the decisions, the alternatives that were
rejected, the risks that were accepted, and what building it taught us. Anything here that changes must
be changed deliberately, with the reasoning updated.

## Decisions

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| Domain has zero package references | Put the entities in the storage project | The accounting rules are the risky part; keeping them dependency-free makes them testable and portable |
| `Amount` as a `decimal` value object with a `Currency` | `double`, or a raw `decimal` | Rounding and mixed-currency bugs are the classic accounting defect; the type prevents them |
| `Ledger` in memory, statements derived | SQL aggregation per statement | Keeps the accounting logic in one testable place and independent of the storage engine |
| Store amounts as `DECIMAL`, dates as ISO-8601 text | Store `DateTime` | ISO text sorts chronologically and is unambiguous across cultures |
| Index and table names kept short | Descriptive long names | SharpCoreDB derives metadata block names from them and caps a block name at 31 characters |
| Cash flow classified from counterpart accounts | A configurable classification per account | Fewer knobs, an explicit documented rule, and it can be tested |
| HTML + browser print instead of server-side PDF | Bundling a headless browser | Keeps the package small and cross-platform; the HTML is already print-ready |
| Thin Razor Pages instead of a JavaScript SPA | Blazor or a front-end framework | No Node.js, no build step, server-rendered and localized by request culture |
| One SharpCoreDB handle as a singleton, writes serialized | Connection per request | The embedded engine is not designed for concurrent writers; one administration per file makes that honest |
| `Ledger.Restore` for rehydration, `Ledger.Post` for new entries | One method for both | Historical entries may reference accounts that have since been deactivated; the "must be active" rule belongs to posting |
| Import key = bank account + bank reference | Key on the movement's content or a database row id | A bank reference is the only stable identifier the bank itself supplies; content keys would merge genuinely distinct movements |
| `SYN-` content-derived reference as a fallback | Ask the user to resolve duplicates | Import must stay idempotent without extra work; the trade-off is documented in `spec.md` (Edge Cases) |
| xunit.v3 built-in assertions, no assertion library | FluentAssertions, Shouldly | Keeps every referenced package free and permissively licensed, so the suite is usable in any context |
| Spec Kit for the design artifacts | BMAD phases, or ad-hoc design docs | Spec Kit keeps the specification, plan, tasks and decisions in one place and drives them from the agent with slash commands |

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| The encrypted database's master password is lost | Documented loudly - in the README and at start-up: the password is the only key, it is never stored, losing it means losing the data |
| Cash-flow classification is inherently judgemental | The rule is explicit and derived (counterpart account type), documented here and in `plan.md`, and covered by tests rather than exposed as configuration |
| Rewriting a spreadsheet user's mental model is hard | Keep the vocabulary and the statements conventional; use standard accounting terms (balance sheet, profit and loss, cash flow) |
| Embedded storage limits concurrency | One administration per file, a single long-lived connection and serialized writes - documented as a design constraint rather than hidden |
| Scope creep towards a full ERP | The out-of-scope list in `spec.md` is the contract; changes start with a new specification |
| A bank changes its export format | Readers follow the format, not the bank: XML by local element name, CSV by normalized header name, delimiter and number notation detected per file |
| A dependency turns non-free | Every runtime and test package is MIT/Apache-2.0; the single source-available analyzer is build-time only and justified in `Directory.Build.props` and the constitution |

## What Building It Taught Us

Where the design met reality, and what changed as a result:

1. **SharpCoreDB block names are capped at 31 characters.** Table and index names now carry a comment
   explaining why they are short, and the schema is the single place that knows the DDL.
2. **The Razor report engine needs `DiagnosticSource` *and* `DiagnosticListener`.** Both are registered
   with `TryAdd` so the engine renders outside a web host - a real improvement over the original design,
   where only the web host could render a report.
3. **`ResourcesPath` and the marker type's namespace can silently double up**, making every localization
   lookup miss. The convention is documented in `plan.md` (Web layer) and guarded by `LocalizationTests`,
   which fails loudly when a key does not resolve.
4. **Cash-flow classification needed an explicit rule rather than configuration.** Deriving it from the
   counterpart account type made it testable and kept the configuration surface small.
5. **A statement without bank references cannot be identified by reference.** The import key is the bank
   account plus the bank's reference, so a movement without one now gets a content-derived `SYN-`
   reference. Re-importing the same file stays safe, at the price that two movements sharing their date,
   amount, currency and text count as one - the honest trade-off of not asking the user to resolve
   duplicates.
6. **CSV columns are found by header name, not by position.** Every bank exports its own column order and
   its own number notation; matching normalized header names (and detecting the delimiter per file) reads
   both Dutch and English exports without a bank-specific reader.
7. **A proposal per movement is an N+1 waiting to happen.** The import page first asked the assistant for
   every movement separately, so the ledger and the learned rules were read again and again for the same
   two tables. Proposals are now built in one pass (`BankStatementService.ProposalsAsync`), and a test
   asserts the read count so a later refactor cannot quietly bring the extra reads back.

## Open Questions (resolved)

| Question | Resolution |
| --- | --- |
| Should statements be computed in SQL? | No - an in-memory ledger keeps the rules in one testable place and reports need history older than the period anyway |
| Should cash-flow classification be configurable per account? | No - a derived rule is fewer knobs, documented and testable |
| Should the report engine generate PDFs? | No - print-ready HTML plus CSV covers the need; a custom `IReportRenderer` is the extension point |
| Should imports be transactional across two statements? | Not needed - the import key makes re-import idempotent, which is the property that matters |
| Should the design artifacts keep the earlier BMAD shape? | No - the repository now uses Spec Kit (`spec.md`, `plan.md`, `tasks.md`, `research.md`), with the principles in `.specify/memory/constitution.md` |
