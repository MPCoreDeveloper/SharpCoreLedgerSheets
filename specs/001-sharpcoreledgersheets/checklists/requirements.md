# Requirements Quality Checklist: SharpCoreLedgerSheets 1.0

**Purpose**: a reviewer's pass over `spec.md` to confirm the requirements are complete, unambiguous,
consistent, measurable and traceable - before they are treated as the contract.
**Created**: 2026-11-08
**Feature**: [`spec.md`](../spec.md)

**Review Ownership**: reviewer-owned requirements-quality review artifact. `[x]` means the criterion has
been reviewed and satisfied for requirements quality; it does not mean implementation work is complete
(that is `tasks.md`).

## Completeness

- [x] CHK001 Every user story names its priority, its value and how it can be tested on its own
- [x] CHK002 Every functional requirement area (ledger, statements, storage, reports, web, localization, bank import) has at least one requirement
- [x] CHK003 Non-functional requirements state the architectural, build and platform constraints explicitly
- [x] CHK004 Edge cases cover the failure modes a bookkeeping user actually hits (unbalanced entry, deactivated account, duplicate post, zero denominator, mixed currency, missing password)
- [x] CHK005 Out-of-scope items are listed, so scope creep has a named boundary to argue against

## Clarity

- [x] CHK006 Requirements are written in observable terms ("is rejected", "is presented as positive") rather than internal design terms
- [x] CHK007 No requirement leaves a `NEEDS CLARIFICATION` marker - every open question is resolved in `research.md`
- [x] CHK008 Accounting vocabulary is used conventionally (assets, liabilities, equity, result, operating/investing/financing)

## Consistency

- [x] CHK009 The three statements are described as views of one ledger, so no requirement can make them disagree
- [x] CHK010 Reconciliation requirements (assets = liabilities + equity + result; opening cash + movement = closing cash) appear both as requirements and as success criteria
- [x] CHK011 The bank import requirements agree on one identity rule (bank account + bank reference, with the derived `SYN-` fallback)
- [x] CHK012 Localization requirements agree on eight cultures with Dutch as default and fallback

## Measurability & Traceability

- [x] CHK013 Every success criterion names how it is proven (domain tests, infrastructure tests, rendering tests, localization tests, CI)
- [x] CHK014 Every functional requirement names the test or artifact that verifies it
- [x] CHK015 Success criteria are technology-agnostic at the spec level; the technology lives in `plan.md`
- [x] CHK016 The plan's Constitution Check maps each principle to a gate with a result
- [x] CHK017 Every task in `tasks.md` names the evidence that closes it

## Coverage

- [x] CHK018 All four user stories are represented in `tasks.md` phases and can be demonstrated independently
- [x] CHK019 The post-1.0 backlog is recorded, so deferred scope is visible rather than implied
- [x] CHK020 Assumptions are stated (one administration per file, single user, serialized writes, password is the only key) instead of being hidden in code

## Notes

- Mark items `[x]` only after review confirms the criterion is satisfied; leave them unchecked while they
  still need clarification or correction.
- `checklists/requirements.md` has a separate built-in lifecycle maintained by `/speckit-specify` and
  `/speckit-clarify`.
- Items are numbered sequentially for easy reference in review comments.
