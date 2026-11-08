# Research & Decisions: SharpCoreLedgerSheets rebrand, English schema, Dutch-first UX

**Input**: `spec.md` and `plan.md` for `specs/002-rebrand-and-english-schema/`

**Purpose**: why the rebrand, the schema language and the default culture are what they are, and what was
deliberately deferred.

## Decisions

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| The name is `SharpCoreLedgerSheets`, one word, no separator | `SharpCore.LedgerSheets`, `SharpCore LedgerSheets` | It matches the logo wordmark and the SharpCoreDB family; one token keeps namespaces, package IDs and file names consistent |
| Full rename including namespaces and package IDs | Rename only the display name and README | A half rename leaves `LedgerSheets.*` assemblies in a product called SharpCoreLedgerSheets, which is exactly the confusion a rebrand is meant to end |
| Storage schema becomes English | Keep the Dutch schema (1.0 compatibility) | The schema is the most public part of the storage contract and an international contributor must be able to read it; the product is days old, so the break is cheap now and expensive later |
| Tables are `accounts`, `entries`, `entry_lines`, `banktxns`, `matchrules` | `rekeningen`, ... or fully spelled `bank_transactions`, `counter_account_rules` | English is the point; the shortened forms keep derived metadata block names under SharpCoreDB's 31-character cap |
| Columns are English and self-describing (`is_cash_equivalent`, `counterparty`) | Abbreviations (`is_cm`, `cp`) | Column names do not affect block names, so clarity is free here |
| Version goes to 2.0.0 | Keep 1.0.x | The on-disk schema and the package IDs change; a major version is the honest signal |
| No migration shim from 1.0 files | Dual-schema repositories | Two schemas alive at once doubles the query surface for data that can be re-imported in minutes; the upgrade note says so explicitly |
| Dutch stays the default culture | English default with Dutch as an option | The target market is the Dutch MKB: the first run must greet a Dutch entrepreneur in Dutch, with Dutch number and date formatting |
| The default culture is configurable | Hard-code `nl` | One line in `appsettings.json` lets the same build serve another market without a code change |
| English remains the neutral fallback | Make Dutch the neutral resource file | The neutral file is the last resort for any missing key; English is the safest neutral for an international audience, and every Dutch key ships translated |
| Rule messages stay Dutch for now | Translate all engine messages to English | Dutch is the primary market and the messages are user-facing; the correct fix is message keys plus resources, not a different hard-coded language - tracked below |
| Logo derived to PNG at 512/192/128/64 px | Commit the 1.5 MB source JPG | A README and a web header should not download a 2048×2048 JPEG; the source stays in the (git-ignored) `Artifacts/` folder |
| `Artifacts/` stays untracked, `assets/logo/` is tracked | Track `Artifacts/` | `artifacts/` is already the build-output convention in `.gitignore`; the generated brand assets belong in `assets/` |

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Existing 1.0 `.scdb` files cannot be opened | Documented as a breaking change in `README.md` ("Upgrading from LedgerSheets 1.0") and in the spec's edge cases; users start a new file and re-import |
| The GitHub repository has not been renamed yet | Badges, `RepositoryUrl` and `PackageProjectUrl` already point at `MPCoreDeveloper/SharpCoreLedgerSheets`; rename the repository before publishing the 2.0 package |
| A table name could exceed SharpCoreDB's block-name cap | Names stay short by rule, the rationale is written next to the DDL, and the infrastructure tests create real databases and round-trip every repository |
| A long mechanical rename could silently change a string that mattered | The suite (330 tests, including rendering and localization) ran after the rename, and `git grep` was used to prove no `LedgerSheets` reference or Dutch schema identifier was left behind |
| Dutch-first could turn into Dutch-only | The culture remains selectable per user (query string, cookie, browser header) and every key is proven in all eight cultures by `LocalizationTests` |

## Open Questions (resolved)

| Question | Resolution |
| --- | --- |
| Should the brand keep "LedgerSheets" anywhere? | Only in the historical upgrade note and in the earlier feature record; every current surface says SharpCoreLedgerSheets |
| Should the schema keep Dutch aliases for one release? | No - aliases would need dual DDL and dual queries; a clean break with a clear upgrade note is smaller and honest |
| Is English as the neutral fallback a problem for Dutch users? | No - the `nl` translations are complete (guarded by tests), so the neutral file is only reached for a key that has not been translated yet |
| Should the logo be embedded in the report HTML? | Not yet - reports stay text-only and print-ready; the logo is a web/README asset, and a report watermark is a backlog idea |

## Backlog (post-2.0, ordered)

**Shipped in 2.1** (`specs/003-localized-messages-and-starter-chart/`): the message catalogue (Dutch and
English) with `MessageCode`-based translation, the embedded brand mark in reports, and the two starter
charts of accounts - the Dutch MKB chart and its international twin.

Still open:

1. **Translate the message catalogue** for `de`, `fr`, `es`, `it`, `pt` and `pl` - those cultures show
   English messages today, while their interface is fully translated.
2. ~~**VAT (btw) support**~~ - shipped in 2.2 (`specs/005-correct-books/`): per-rate VAT accounts, VAT booking
   for sales and purchases, and a quarterly return.
3. **Full RGS alignment** - offer the complete Referentie GrootboekSchema behind a third starter chart.
4. **In-place upgrade tool** - only if 1.0 data turns out to matter in practice: read the Dutch tables and
   write the English ones once, then drop the old tables.
