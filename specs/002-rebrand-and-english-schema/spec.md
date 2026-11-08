# Feature Specification: SharpCoreLedgerSheets rebrand, English schema and Dutch-first UX

**Feature Branch**: `main` (shipped as SharpCoreLedgerSheets 2.1.0)

**Created**: 2026-11-08

**Status**: Implemented

**Input**: Rebrand the product from LedgerSheets to SharpCoreLedgerSheets (logo supplied in
`Artifacts/`), make the code base international by giving the storage schema English table, column and
index names, and keep - and make explicit - the Dutch option the Dutch MKB market needs.

## Context

LedgerSheets 1.0 spoke Dutch to the inside as well as the outside: the database schema used Dutch
table and column names (`rekeningen`, `posten`, `postregels`, `bankmutaties`, `matchregels`). That made
the most public part of the storage contract unreadable for anyone outside the Netherlands.

The product is now **SharpCoreLedgerSheets**, part of the SharpCoreDB family. The inside is English -
code, API, schema - while the outside stays **Dutch first**, because the market that pays for it is the
Dutch MKB. Dutch is the default culture, and every other supported language is one click away.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A Dutch entrepreneur opens the app (Priority: P1)

Sanne, a freelancer in Utrecht, installs the application and opens it. Without configuring anything she
gets a Dutch interface, Dutch number formatting (`1.234,56`), Dutch dates (`31-12-2026`) and Dutch
report labels (*Balans*, *Winst- en verliesrekening*, *Kasstroomoverzicht*).

**Why this priority**: The Dutch MKB is the target market; a first run that greets the user in another
language is a lost sale.

**Independent Test**: start the app with a master password and `GET /` - the response contains Dutch
labels and `lang="nl"`. Guarded by `LocalizationTests.Dutch_IsTheDefaultCulture`.

**Acceptance Scenarios**:

1. **Given** a fresh installation with no configuration, **When** the dashboard is requested,
   **Then** the page renders in Dutch and the language picker highlights `nl`.
2. **Given** the same installation, **When** a host sets
   `SharpCoreLedgerSheets:Localization:DefaultCulture` to `en`, **Then** the default becomes English
   without a code change.

---

### User Story 2 - An international contributor reads the data model (Priority: P2)

Daan, a developer outside the Netherlands, opens the repository and the database schema. Every table,
column and index has an English, self-describing name, so he can write a query or a reader without a
Dutch dictionary.

**Why this priority**: A rebrand to an international name is only credible if the storage contract is
readable; it is also the part of the code that is hardest to change later.

**Independent Test**: the infrastructure tests create real encrypted databases from the schema and
round-trip every repository, so the English names are proven, not just declared.

**Acceptance Scenarios**:

1. **Given** the schema, **When** it is applied to a new database, **Then** it creates `accounts`,
   `entries`, `entry_lines`, `banktxns` and `matchrules` with English columns.
2. **Given** a stored administration, **When** the database is closed and reopened, **Then** every
   account, entry, movement and matching rule is read back unchanged.

---

### User Story 3 - An international user switches language (Priority: P3)

A German or English bookkeeper selects a language in the top right (or arrives with `?culture=de`). The
interface and the reports follow, and the choice is remembered.

**Why this priority**: The Dutch option must not become the only option; the product is international
too.

**Independent Test**: `GET /?culture=en` returns English content; the localization tests prove every
key resolves in all eight cultures and that an unsupported culture falls back to the neutral English
file.

**Acceptance Scenarios**:

1. **Given** any page, **When** `?culture=de` is requested, **Then** the page and the reports are
   German and the cookie remembers the choice.
2. **Given** a culture without translations for a key, **When** the key is resolved, **Then** the
   English neutral value is shown instead of a missing-key marker.

---

### User Story 4 - A branded installation (Priority: P4)

Anyone who lands on the repository or the running application recognises it: the SharpCoreLedgerSheets
logo in the README, in the web header and as the favicon, and the name is consistent in the metadata,
the package IDs, the configuration section and the assembly names.

**Why this priority**: A rebrand that leaves old names in configuration files or package IDs is a
half-finished rebrand that confuses operators.

**Independent Test**: the logo image is served with HTTP 200 by the web host, the README renders it
from the repository, and `git grep` finds no `LedgerSheets` reference left outside the historical
upgrade note.

**Acceptance Scenarios**:

1. **Given** the repository, **When** the README is opened, **Then** the logo and the new tagline appear
   at the top.
2. **Given** a running instance, **When** any page is loaded, **Then** the header shows the logo and the
   product name, and `/img/logo.png` returns 200.

---

### Edge Cases

- An old 1.0 `.scdb` file (Dutch schema) opened by 2.0 finds no `accounts` table. There is no in-place
  migration: the upgrade note tells the user to start a new file and re-import, or to stay on 1.0.
- A configuration that still uses the old section name (`LedgerSheets:Storage`) is ignored - the master
  password is then missing and the start-up warning names the new key.
- A culture name that is not supported in `SharpCoreLedgerSheets:Localization:DefaultCulture` falls
  back to Dutch instead of failing at start-up.
- A table name longer than the SharpCoreDB block-name limit would break the schema; names stay short by
  rule (`banktxns`), and the infrastructure tests prove they work.

## Requirements *(mandatory)*

### Functional Requirements

#### FR-1 Brand

| ID | Requirement |
| --- | --- |
| FR-1.1 | The product, assemblies, namespaces, solution, package IDs, configuration section and environment-variable prefix are `SharpCoreLedgerSheets` |
| FR-1.2 | The logo ships in `assets/logo/` (512 and 128 px PNG, derived from `Artifacts/SharpCoreLedgerSheets.jpg`) and is shown in the README |
| FR-1.3 | The web application shows the logo in the header and as favicon (`wwwroot/img/logo.png`, `wwwroot/img/favicon.png`), and the title/footer use the localized product name |
| FR-1.4 | Package metadata (`Product`, `RepositoryUrl`, version `2.0.0`) and the specification/plan documents use the new name |

#### FR-2 English schema

| ID | Requirement |
| --- | --- |
| FR-2.1 | Every table, column and index name is English: `accounts`, `entries`, `entry_lines`, `banktxns`, `matchrules` |
| FR-2.2 | Column names are English and self-describing (`number`, `name`, `type`, `is_active`, `is_current`, `is_cash_equivalent`, `is_inventory`, `date`, `description`, `reference`, `entry_id`, `sequence`, `account_id`, `debit`, `credit`, `bank_account`, `amount`, `currency`, `counterparty`, `iban`, `status`, `keyword`, `counter_account`, `confirmations`) |
| FR-2.3 | Names stay within the SharpCoreDB block-name limit (31 characters) and the reason is documented next to the DDL |
| FR-2.4 | The repositories, their SQL and their parameters use the same English names, with no Dutch identifiers left in the storage layer |
| FR-2.5 | Dutch leftovers in the domain and application code (`post` locals, `kasMovement`, `ReadOptioneleText`) are renamed to English |

#### FR-3 Dutch-first localization

| ID | Requirement |
| --- | --- |
| FR-3.1 | Dutch is the default culture (`Languages.Default = "nl"`), listed first in the language picker |
| FR-3.2 | The default can be changed per host with `SharpCoreLedgerSheets:Localization:DefaultCulture`, validated against the supported cultures |
| FR-3.3 | English stays the neutral fallback; `de`, `fr`, `es`, `it`, `pt` and `pl` remain fully translated |
| FR-3.4 | The language choice persists (query string, then cookie, then browser header) |

### Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-1 | No new package dependencies for the rebrand or the schema rename |
| NFR-2 | The build stays warning-free and the architecture tests keep guarding the layering |
| NFR-3 | The whole suite stays green, including the real-database round-trips |
| NFR-4 | The logo files stay small enough for a repository and a web page (PNG, tens of kilobytes) |

## Success Criteria *(mandatory)*

- **SC-001**: `dotnet build SharpCoreLedgerSheets.slnx -warnaserror` succeeds with 0 warnings.
- **SC-002**: `dotnet test SharpCoreLedgerSheets.slnx` passes - 330 tests, including the schema
  round-trips and the Dutch-default guard.
- **SC-003**: `GET /` renders Dutch content and `lang="nl"`.
- **SC-004**: `GET /?culture=en` renders English content.
- **SC-005**: No Dutch table, column or index identifier remains in the storage layer.
- **SC-006**: `/img/logo.png` and `/img/favicon.png` return 200 from the running web application.

## Assumptions

- The GitHub repository will be renamed to `SharpCoreLedgerSheets`; the badges and metadata already
  point at that URL.
- Existing 1.0 data is not migrated automatically: the on-disk schema changes and the product is young
  enough that a documented clean break (new file, re-import) is cheaper than a migration shim.
- Rule messages raised in the engine remain Dutch for now (the target market is Dutch); localizing them
  through resources is the first backlog item, tracked in `research.md`.

