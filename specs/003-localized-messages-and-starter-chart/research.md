# Research & Decisions: Localized messages, branded reports, starter charts

**Input**: `spec.md` and `plan.md` for `specs/003-localized-messages-and-starter-chart/`

## Decisions

| Decision | Alternative rejected | Why |
| --- | --- | --- |
| A message carries a **code plus a Dutch fallback message** | Codes only; or translating inside the domain | The domain may not depend on resources (principle I), and a code alone would show "AccountNotFound" to a user when a host has no translation. The Dutch message is the primary market's text and the last-resort fallback |
| The **Web layer** translates, through `IStringLocalizer` | Translator injected into the domain services | Keeps the layering intact: the domain raises, the host speaks |
| Catalogue keys are `Msg_<code>` in the **shared resources** | A separate message resource file | One localizer, one completeness guard, one place a translator has to look |
| Both Dutch and English catalogues ship complete | English only, with Dutch read from the exception | `ResourceManagerStringLocalizer` falls back culture -> neutral, so a Dutch user would have read English messages; the Dutch keys must exist explicitly |
| The other six cultures fall back to English for **messages** | Machine-translating 53 sentences now | Their interface is translated; a wrong translation in an error message is worse than a correct English one. Recorded as a backlog item |
| Placeholders are `{0}`-style and formatted with `CurrentCulture` | String concatenation in the exception | Keeps the numbers, dates and amounts in the user's notation, and lets a translator move them around |
| A broken translation falls back to the exception's message | Letting the `FormatException` escape | A localization defect must never hide the accounting failure the user needs to fix |
| The brand mark is an **embedded resource -> data URI** | Linking `wwwroot/img/logo.png`; or embedding the 512 px logo | A report must print offline as one file; 64 px keeps every document small (the base64 grows by ~10 KB, not ~450 KB) |
| The logo lives in the Reports assembly, not the Web app | Reusing the web favicon at runtime | The Reports project must render a branded document on its own, without the web host |
| Two starter charts with the **same numbering** | One chart per language with its own numbering | The numbering is the bookkeeping contract; only the names should change with the market, so a switch never mixes two charts |
| `ApplyStarterChartAsync` **adds only what is missing** | Fail when an account exists; or overwrite | A half-filled administration is the normal case: it must be able to top up without losing its own edits |
| Starter charts live in the **domain** | A JSON file read by the application layer | They are accounting content, testable without I/O, and small enough to be code |

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| A new message is added without translations | `EveryMessageCode_HasATranslationForDutchAndEnglish` fails the build for the missing catalogue entry |
| A translation has a wrong placeholder index | `ExceptionMessages.Describe` catches `FormatException` and shows the engine's message instead |
| The Dutch catalogue is forgotten, so Dutch users see English | The same guard test requires every code in `nl` and in the neutral file |
| The starter chart drifts from the domain rules | `For_ProducesAccountsTheDomainAccepts` builds an `Account` for every template row, so an invalid flag combination fails in a test |
| Applying a chart twice duplicates accounts | `ApplyStarterChartAsync_IsIdempotent` and `..._KeepsAccountsTheAdministrationAlreadyHas` |

## Backlog (post-2.1, ordered)

1. **Translate the message catalogue** for `de`, `fr`, `es`, `it`, `pt` and `pl` (53 sentences each) so an
   error message is never in another language than the interface.
2. **VAT (btw) support** - rates per line, a VAT return summary and VAT payable/receivable accounts; the
   Dutch chart already reserves `2100 Te betalen btw`.
3. **RGS alignment** - offer the full Referentie GrootboekSchema instead of the simplified chart, behind a
   second template.
4. **Report cover page** - an optional branded first page per report, reusing the embedded mark.
