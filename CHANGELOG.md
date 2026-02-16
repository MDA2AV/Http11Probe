# Changelog

All notable changes to Http11Probe are documented in this file.

## [2026-02-16]

### Added
- **Baseline test gate** — probe workflow now fails when a server can't pass `COMP-BASELINE` or `COMP-POST-CL-BODY`, blocking merge; PR comment shows prominent pass/fail status and is posted even on failure (#98)
- **4 caching tests** — `CAP-IMS-FUTURE` (If-Modified-Since with future date), `CAP-IMS-INVALID` (invalid IMS value), `CAP-INM-UNQUOTED` (unquoted ETag in If-None-Match), `CAP-ETAG-WEAK` (weak ETag validation) (#94)
- **Sequence tests** — new multi-step test infrastructure (`SequenceTestCase`, `SequenceStep`, `SequenceSendPart`) for desync and smuggling detection with timed partial sends and behavioral analysis (#74)
- **26 new smuggling tests** — multi-step sequence tests for CL.TE, TE.CL, and desync detection:
  - `SMUG-CLTE-SMUGGLED-GET` — CL.TE with embedded GET; multiple responses indicate boundary confusion
  - `SMUG-CLTE-SMUGGLED-HEAD` — CL.TE with embedded HEAD
  - `SMUG-CLTE-SMUGGLED-GET-CL-PLUS` — CL.TE smuggled GET with malformed CL (+N)
  - `SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC` — CL.TE smuggled GET with non-numeric CL
  - `SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD` — CL.TE smuggled GET with obs-folded TE
  - `SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE` — CL.TE smuggled GET with TE trailing space
  - `SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA` — CL.TE smuggled GET with TE leading comma
  - `SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH` — CL.TE smuggled GET with TE case mismatch
  - `SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET` — duplicate TE headers with embedded GET
  - `SMUG-TECL-SMUGGLED-GET` — TE.CL with embedded GET (chunk-size prefix trick)
  - `SMUG-DUPLICATE-CL-SMUGGLED-GET` — duplicate Content-Length with embedded GET
  - `SMUG-GET-CL-PREFIX-DESYNC` — GET with CL prefix desync
  - `SMUG-CLTE-DESYNC` — CL.TE desync with pause-based detection
  - `SMUG-TECL-DESYNC` — TE.CL desync with pause-based detection
  - `SMUG-CLTE-CONN-CLOSE` — CL.TE desync with Connection: close
  - `SMUG-TECL-CONN-CLOSE` — TE.CL desync with Connection: close
  - `SMUG-PIPELINE-SAFE` — safe pipeline baseline (no smuggling)
  - `SMUG-CL0-BODY-POISON` — CL:0 body poison follow-up check
  - `SMUG-GET-CL-BODY-DESYNC` — GET with CL body desync
  - `SMUG-OPTIONS-CL-BODY-DESYNC` — OPTIONS with CL body desync
  - `SMUG-EXPECT-100-CL-DESYNC` — Expect: 100-continue CL desync
  - `SMUG-OPTIONS-TE-OBS-FOLD` — OPTIONS with obs-fold TE follow-up check
  - `SMUG-CHUNK-INVALID-SIZE-DESYNC` — invalid chunk size + poison follow-up
  - `SMUG-CHUNK-EXT-INVALID-TOKEN` — invalid token in chunk extension name
  - `SMUG-CHUNK-SIZE-PLUS` — chunk size with leading plus sign
  - `SMUG-CHUNK-SIZE-TRAILING-OWS` — chunk size with trailing whitespace
- **11 new compliance tests**:
  - `COMP-RANGE-POST` — Range header on POST should be ignored (RFC 9110 §14.2)
  - `COMP-UPGRADE-HTTP10` — Upgrade header in HTTP/1.0 request
  - `COMP-DATE-FORMAT` — Date header format validation (RFC 9110 §5.6.7)
  - `COMP-VERSION-CASE` — HTTP version case sensitivity (RFC 9112 §2.6)
  - `COMP-LONG-URL-OK` — long URL within valid range should be accepted
  - `COMP-SPACE-IN-TARGET` — space in request target should be rejected
  - `COMP-DUPLICATE-CT` — duplicate Content-Type headers
  - `COMP-TRACE-SENSITIVE` — TRACE method security sensitivity (RFC 9110 §9.3.8)
  - `COMP-RANGE-INVALID` — invalid Range header format
  - `COMP-ACCEPT-NONSENSE` — nonsensical Accept header value
  - `COMP-POST-UNSUPPORTED-CT` — POST with unsupported Content-Type
- **FastEndpoints framework** — new test server added to the probe suite (#70)
- **Local probe script** — `scripts/probe-local.sh` for running probes against local servers
- **Sequence tests UI** — probe results page displays sequence test steps with per-step request/response details

### Changed
- **Transposed result tables** — rows are now test IDs and columns are servers (previously the reverse), making tall tables with fewer columns (#97)
- **SMUG-CLTE-PIPELINE and SMUG-TECL-PIPELINE** — re-evaluated scoring and validation logic
- **GenHTTP server** — clean-up and simplification (contributed by Andreas Nägeli)
- **RFC Requirement Dashboard** — updated with all 37 new tests and counts

### Fixed
- **Traefik server** — fixed POST / to echo request body (contributed by SAILESH4406, #79)
- **Sequence test UI rendering** — fixed display of multi-step test results on probe results page
- **Second read from wire** — improved response capture with additional socket read for slow/partial responses (#71)
- **PR comment score** — fixed score calculation in probe workflow CI comments
- **NGINX server** — fixed implementation (#63)

## [2026-02-14]

### Added
- **RFC Level indicator row** — result tables now show a translucent capsule (MUST/SHOULD/MAY/N/A) for each test, indicating the RFC 2119 requirement level
- **Method indicator row** — result tables show the HTTP method (GET, POST, etc.) for each test in an outlined monospace badge style
- **Method filter** — filter result tables by HTTP method (GET, POST, HEAD, etc.) on all category pages
- **RFC Level filter** — filter result tables by RFC requirement level (MUST, SHOULD, MAY, N/A) on all category pages
- **Method & RFC Level in popup** — server detail modal now includes Method and RFC Level columns alongside Test, Expected, Got, and Description
- **`RfcLevel` enum** — `Must`, `Should`, `May`, `OughtTo`, `NotApplicable` classification for every test case
- **RFC Level annotations** — all tests across Compliance, Smuggling, MalformedInput, and Normalization suites annotated with their RFC 2119 requirement level
- **Verbose Probe workflow** — new `probe-verbose.yml` GitHub Action for manual single-server probing with `--verbose` output, triggered via `workflow_dispatch` with a server name input (#60)
- **Giscus comments** — added comment system to website documentation pages
- **AI Contribution guide** — `AGENTS.md` for AI-agent contributions and `add-with-ai-agent` docs page
- **RFC Requirement Dashboard page** — comprehensive per-test RFC requirement tracking with counts and cross-references
- **9 new RFC 9110 compliance tests** sourced from [mohammed90/http-compliance-testing](https://github.com/mohammed90/http-compliance-testing):
  - `COMP-HEAD-NO-BODY` — HEAD response must not contain a message body (RFC 9110 §9.3.2, MUST)
  - `COMP-UNKNOWN-METHOD` — unrecognized method should be rejected with 501/405 (RFC 9110 §9.1, SHOULD)
  - `COMP-405-ALLOW` — 405 response must include Allow header (RFC 9110 §15.5.6, MUST)
  - `COMP-DATE-HEADER` — origin server must include Date header in responses (RFC 9110 §6.6.1, MUST)
  - `COMP-NO-1XX-HTTP10` — server must not send 1xx to HTTP/1.0 client (RFC 9110 §15.2, MUST NOT)
  - `COMP-NO-CL-IN-204` — Content-Length forbidden in 204 responses (RFC 9110 §8.6, MUST NOT)
  - `SMUG-CL-COMMA-TRIPLE` — three comma-separated identical CL values (RFC 9110 §8.6, unscored)
  - `COMP-OPTIONS-ALLOW` — OPTIONS response should include Allow header (RFC 9110 §9.3.7, SHOULD)
  - `COMP-CONTENT-TYPE` — response with content should include Content-Type (RFC 9110 §8.3, SHOULD)

### Changed
- **AGENTS.md** — added Step 5 (RFC Requirement Dashboard) to the "Add a new test" task; added Step 5 (server documentation page) to the "Add a framework" task
- **RFC Requirement Dashboard** — updated with all 9 new tests, counts, and cross-references
- **Landing page cards** — removed hardcoded test count from RFC Requirement Dashboard subtitle
- **Score calculation** — warnings now included in the overall score (#66)

### Fixed
- **Caddy server** — fixed POST body echo using Caddy Parrot pattern; updated Caddyfile, Dockerfile, and docs page
- **Lighttpd server** — fixed POST body echo implementation (#57)
- **HAProxy server** — fixed POST / endpoint (#64)
- **Echo validation** — empty body now correctly returns Fail; body mismatch returns Fail; chunked transfer encoding properly decoded before comparison (#61)
- **Validator ordering** — fixed 8 tests where connection-state check ran before response-status check, preventing false passes when server returned 2xx then closed (COMP-POST-CL-UNDERSEND, RFC9112-2.3-HTTP09-REQUEST, MAL-BINARY-GARBAGE, MAL-INCOMPLETE-REQUEST, MAL-EMPTY-REQUEST, MAL-WHITESPACE-ONLY-LINE, MAL-H2-PREFACE, MAL-POST-CL-HUGE-NO-BODY)
- **COMP-CHUNKED-NO-FINAL validator** — fixed same ordering bug where connection close was accepted even when server returned 2xx
- **Method extraction** — handles leading CRLF in raw requests and tab-delimited request lines; non-HTTP pseudo-methods (PRI) shown as '?'
- **Category-scoped filters** — Method and RFC Level filters now only show options relevant to the current category page

## [2026-02-13]

### Added
- **Server configuration pages** — per-server docs pages showing Dockerfile, source code, and config files for all 36 tested servers (`docs/content/servers/`) (#28)
- **Clickable server names** — server names in the probe results table and summary bar chart now link to their configuration page
- **Sticky first column** — server name column stays pinned to the left edge while scrolling horizontally through result tables
- **Collapsible sub-groups** — group headers in result tables are now clickable to collapse/expand, with a chevron indicator and a "Collapse All / Expand All" toggle button
- **Row-click detail popup** — clicking a server row opens a modal showing that server's results for the current table in a vertical layout (Test, Expected, Got, Description) with section and table name in the header
- **Truncation notice** — tooltip and modal now show a `[Truncated]` notice at the top when raw request/response data exceeds the 8,192-byte display limit
- **Header normalization section** — new test category for header normalization tests (#32)
- **"Add a Framework" section improvements** — expanded documentation for adding new server frameworks (#42)

### Changed
- **Scrollable tooltips** — hover tooltips are now interactive and scrollable for large payloads (removed `pointer-events:none`, increased `max-height` to `60vh`)
- **Larger click modal** — expanded from `max-width:700px` to `90vw` and `max-height` from `80vh` to `85vh` to better accommodate large request/response data
- Raw request capture now includes truncation metadata when payload exceeds 8,192 bytes
- Raw response capture now includes truncation metadata when response exceeds 8,192 bytes
- **Test re-evaluation** — reviewed and re-scored multiple tests for RFC alignment (#29)

### Fixed
- **Kestrel server** — fixed HEAD and OPTIONS headers allowed (#39)
- **Node.js server** — fixed errors in Express server (#37)
- **CLI and PR scores** — fixed score calculation in CLI output and PR comments
- GenHTTP server re-enabled in probe suite

## [2026-02-12]

### Added
- **Request/response detail tooltips** — hover over a result pill to see the raw response; click to open a modal with both the raw request and response (#27)
- Repository cleanup — removed clutter files (probe-glyph.json, pycache, package-lock.json, DotSettings.user)

### Fixed
- BARE-LF tests (RFC 9112 §2.2) adjusted to warn on 2xx instead of fail, matching RFC SHOULD-level requirement (#21)

### Removed
- Proxy compliance tests removed from the suite (#20)

## [2026-02-11]

### Added
- POST endpoint for Kestrel (ASP.NET Minimal) server (#13)
- POST endpoint for Quarkus server (#14)
- POST endpoint for Spring Boot server (#16)
- POST endpoint for Express server (#17)

### Fixed
- H2O server now allows POST commands (#19)
- Flask server routing and default port (#11)
- SimpleW server POST handling and version update (#5)

## [2026-02-09]

### Added
- SimpleW server contributed by stratdev3 (#2)

### Fixed
- Glyph server — reset request state on each new connection (#3)
- In-development frameworks now filtered from results (#4)
- SimpleW removed from blacklisted servers

## [2026-02-08]

### Added
- **30 new tests** — body/content handling, chunked TE attack vectors, and additional compliance/smuggling tests (46 → 80 → 110+)
- **7 new servers** — Actix, Ntex, Bun, H2O, NetCoreServer, Sisk, Watson
- **6 more servers** — GenHTTP, SimpleW, EmbedIO, Puma, PHP, Deno, and others (total: 36)
- **Deep analysis docs** — verified RFC evidence and ABNF grammar added to all glossary pages
- **Exact HTTP request code blocks** in all glossary pages
- **Category filter** — filter probe results by Compliance, Smuggling, or Malformed Input
- **Language filter** — filter servers by programming language
- **Sub-tables** — result tables split into logical groups within each category
- **Unscored tests** — separate bucket for RFC-compliant reference tests, shown with reduced opacity and asterisk
- **CLI improvements** — `--test` filter, `--help`, docs links in output, selected test display
- **Summary bar chart** — ranked bars replacing summary badges, with pass/warn/fail/unscored segments
- **Scrollbar styling** — themed scrollbars for probe result tables
- **Custom favicon** — shield icon for browser tab
- **Docs logo** — minimal shield outline

### Fixed
- Summary fail count derivation so pass + warn + fail = total
- Unscored double-counting in summary statistics
- Sort order: rank by scored pass + scored warn only
- Puma Dockerfile: install build-essential for nio4r native extension
- Deno Dockerfile: use `latest` tag instead of nonexistent `:2`
- FRAGMENT-IN-TARGET re-scored as strict (implicit grammar prohibition)
- Nancy and Nginx failing to start in CI
- All servers bound to `0.0.0.0` for Docker reachability

### Removed
- Redundant SMUG-HEADER-INJECTION test (covered by other smuggling tests)
- Nancy server removed from probe (no probe.json)

## [2026-02-07]

### Added
- **Initial release** — extracted from Glyph11 into standalone Http11Probe repository
- 12 standalone test servers dockerized with Docker Compose
- Sequential probe workflow — one server at a time on port 8080
- CI probe workflow (`.github/workflows/probe.yml`) with STRICT expectations dictionary
- Hugo + Hextra documentation site with glossary, per-test docs, and probe results pages
- Separate pages for Compliance, Smuggling, Malformed Input categories
- Landing page with platform framing and contributor onboarding
- "Add a Framework" documentation page

### Fixed
- Docker image tags lowercased as required
- Git worktree/orphan branch creation for latest-results
- GlyphServer: replaced manual buffer with PipeReader, fixed closing without response on oversized requests
- Pingora build: added cmake and g++ to build stage
