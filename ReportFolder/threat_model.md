# CardPicker2 Repository Threat Model

## Overview

CardPicker2 is a single-user local ASP.NET Core Razor Pages web application for managing meal cards and drawing a meal with slot-machine style interaction. The primary runtime project is `CardPicker2/`; tests, Spec Kit documents, and development notes support the product but are not deployed application surfaces by default.

The app exposes server-rendered Razor Pages, form posts, query-string filters, static assets, localization resources, and a local JSON persistence file at `CardPicker2/data/cards.json`. Core state includes meal card content, card metadata, preference state, draw history, draw statistics, language preference, and temporary UI status messages.

Security review should treat the app as a local single-user web app, not a multi-tenant internet service. Even so, browser-facing inputs are untrusted because any request can supply form fields, route IDs, query strings, cookies, and headers. Local file persistence is security-relevant because corrupted or attacker-influenced JSON can affect all future card operations.

Key grounding files and components:

- `CardPicker2/Program.cs`: service registration, localization, HTTPS redirection, HSTS, production CSP, routing, static assets, and Razor Pages.
- `CardPicker2/Pages/Index.cshtml.cs`: home draw, replay, filter, and preference POST handlers.
- `CardPicker2/Pages/Cards/*.cshtml.cs`: card search, create, edit, delete, detail, and preference handlers.
- `CardPicker2/Pages/Language.cshtml.cs` and `CardPicker2/Services/LanguagePreferenceService.cs`: runtime language cookie and return URL handling.
- `CardPicker2/Services/CardLibraryService.cs`: JSON load, validation, draw, mutation, preference update, draw history, and persistence.
- `CardPicker2/Services/CardLibraryFileCoordinator.cs`: same-process write serialization.
- `CardPicker2/wwwroot/js/site.js` and `CardPicker2/wwwroot/css/site.css`: client-side interaction state and presentation, not authoritative business logic.

## Threat Model, Trust Boundaries, and Assumptions

Assets that matter:

- Integrity of `cards.json`, including card identity, meal type, metadata, preference state, draw history, deleted-card retention, and schema version.
- Correctness and fairness of draw results, including selected meal type, filter criteria, cooldown handling, and idempotent draw operation IDs.
- Availability of card operations when data is valid, and safe blocking behavior when persisted data is corrupted or unsupported.
- Browser security controls for state-changing forms, localized UI, and static assets.
- Confidentiality of local file contents, logs, system prompts, connection strings, secrets, stack traces, and corrupted JSON contents.

Trust boundaries:

- Browser to server: all route values, query strings, form fields, anti-forgery tokens, cookies, and headers are untrusted until validated on the server.
- Server to local JSON file: the persisted file is operator-controlled but can be malformed, stale, or tampered with outside the app.
- Server to logs: structured logs must support diagnostics without recording secrets, full corrupted JSON, system prompts, or sensitive local paths beyond necessary file references.
- Client JavaScript to server: JavaScript may improve UX but cannot be trusted to enforce draw, preference, language, or deletion rules.
- Third-party static libraries under `wwwroot/lib`: bundled client dependencies are shipped assets; version/advisory exposure matters, but vendored internals are secondary unless reachable from app-controlled inputs.
- Development and test artifacts: tests and Spec Kit files are not runtime attack surfaces unless explicitly copied into deployment or used by a privileged automation path.

Attacker-controlled inputs:

- `GET /` query values such as meal type, filters, tags, draw mode, result card ID, operation ID, and rotation settings.
- `POST /?handler=Draw` form values including meal type, coin state, draw mode, operation ID, metadata filters, tags, and cooldown settings.
- `POST /?handler=Preference`, `/Cards?handler=Preference`, and details preference posts, including card IDs and target preference state.
- Card create/edit form fields: names, localized content, descriptions, meal type, metadata, tags, and related enum values.
- Delete confirmation form fields and route card IDs.
- `/Language?handler=Set` culture and return URL inputs.
- Culture cookies, request headers, and static-asset requests.
- Local `cards.json` contents if a local user or external process modifies it.

Operator/developer-controlled inputs:

- `appsettings*.json`, Serilog configuration, launch settings, package versions, seed data, localization resources, and Spec Kit documents.
- Build/test commands and local development tools.

Important assumptions:

- The intended deployment is single-user/local. There is no app-level authentication or authorization boundary between multiple users.
- The app should still resist CSRF, malformed input, XSS, unsafe redirects, sensitive logging, and unsafe file persistence because browsers and local environments can be hostile.
- Razor default output encoding is part of the security model; any explicit raw HTML or script injection surface would need separate proof.
- Production middleware should enforce HTTPS/HSTS and CSP; development behavior can be less restrictive but should not be used as production evidence.
- The JSON path is configured by the app, not by direct user input. User-controlled path traversal is only realistic if a future feature exposes file path selection or import/export paths.

## Attack Surface, Mitigations, and Attacker Stories

Primary attack surfaces:

- Razor Page POST handlers for draw, preference update, create, edit, delete, and language selection.
- Query-string filters that influence search, draw candidates, restored results, and UI state.
- Local JSON load/migration/write logic, including corrupted-file handling and atomic write behavior.
- Localization cookie parsing, cookie creation, and local redirect handling.
- Production response headers, especially CSP, HSTS, frame ancestors, and form action restrictions.
- Client-side code that manipulates form state, reduced-motion behavior, language preservation, theme state, draw animation, and preference action forms.
- Bundled static JavaScript and CSS dependencies.

Existing mitigations and controls to preserve:

- Razor Pages model binding plus service-layer validation for domain rules.
- ASP.NET Core anti-forgery behavior for state-changing Razor form posts.
- Razor HTML encoding by default.
- `LocalRedirect` with service-level return URL normalization for language changes.
- Supported-culture allowlist and fallback for unsupported or mismatched culture cookies.
- Production HSTS, HTTPS redirection, and CSP configured in `Program.cs`.
- JSON document validation, schema checks, corrupted-file blocking, and same-process mutation serialization.
- Service boundaries that keep PageModels from owning core draw, persistence, duplicate, filter, and randomization rules.
- Tests for security headers, anti-forgery-sensitive flows, localization, persistence, draw idempotency, and corrupted-data behavior.

Realistic attacker stories:

- A local webpage or malicious site attempts CSRF against draw, preference, language, create/edit/delete, or card-management forms.
- A crafted request bypasses client UI constraints by posting invalid enum values, IDs, tags, draw operation IDs, cooldown values, or preference states directly.
- A malicious or corrupted `cards.json` attempts to break schema migration, inject unexpected enum/string values, poison localized content, or trigger partial writes.
- A crafted language return URL attempts open redirect or cookie manipulation.
- Stored card text attempts script injection through Razor views or attributes.
- A browser requests static assets or pages in production and relies on missing headers to frame, inject scripts, or exfiltrate data.

Less realistic or out-of-scope attacker stories:

- Tenant isolation and privilege escalation between app users, because the app currently has no multi-user identity model.
- Remote database injection, because the app uses a local JSON file and does not issue SQL/NoSQL/LDAP/XPath queries.
- SSRF, because current runtime code does not fetch attacker-supplied URLs.
- Command injection, because current runtime code does not execute shell commands from user inputs.
- Arbitrary server file read/write via user path input, because current exposed forms do not accept filesystem paths; review should still verify persistence helpers and any future import/export surfaces.

## Severity Calibration (Critical, High, Medium, Low)

Critical findings in this repository would require a severe boundary break despite the local single-user model, such as remote code execution from a browser request, arbitrary file write outside the intended data file with trusted-config impact, or a vulnerability that exposes system prompts, secrets, connection strings, or local sensitive files through the web app.

High findings would include unauthenticated or CSRF-reachable destructive state changes that bypass anti-forgery protection in a realistic browser flow, stored XSS through card/localization content, arbitrary overwrite or corruption of `cards.json` from untrusted web input, open redirect with credential or trust-boundary impact, or production security-header misconfiguration that materially enables script execution or clickjacking.

Medium findings would include server-side validation gaps that allow invalid draw/card states without broader compromise, corrupted JSON handling that blocks availability incorrectly or logs sensitive fragments, weak cookie attributes in realistic deployment, preference or deletion operations that can be forged only under constrained preconditions, or replay/idempotency flaws with integrity impact but limited data exposure.

Low findings would include hardening gaps with limited exploitability in the local single-user context, missing defensive tests for a protected path, verbose but non-secret logs, minor header omissions in development-only scenarios, or UI-only validation mismatches that are still rejected by the service layer.

Suppression guidance:

- Do not report a bare sink unless there is a concrete attacker-controlled source, missing or insufficient control, and security-relevant impact.
- Do not suppress a server-side validation issue merely because client JavaScript or form controls normally prevent the input.
- Do not treat the lack of app authentication as a vulnerability by itself; report only when a concrete security boundary that the app claims to enforce is bypassed.
- Do not treat corrupted local JSON as attacker-controlled remote input unless the data can realistically be written by a browser request or a privileged local process.
