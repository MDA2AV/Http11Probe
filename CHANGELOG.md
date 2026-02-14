# Changelog

All notable changes to Http11Probe are documented in this file.

## [Unreleased]

### Added
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
- **Caddy server** — fixed POST body echo using Caddy Parrot pattern; updated Caddyfile, Dockerfile, and docs page

## [2026-02-14]

### Added
- **RFC Requirement Dashboard** — all 148 tests classified by RFC 2119 level (MUST/SHOULD/MAY/"ought to"/Unscored/N/A) with exact RFC quotes proving each classification (`docs/content/docs/rfc-requirement-dashboard.md`)
- **Add a Test guide** — step-by-step documentation for contributing new tests to the platform (`docs/content/add-a-test.md`)
- **AI Agent contribution guide** — machine-readable `AGENTS.md` at repo root with precise instructions for LLM agents to add tests or frameworks (`docs/content/add-with-ai-agent.md`)
- **Contribute menu** — top nav "Add a Framework" replaced with a "Contribute" dropdown containing Add a Framework, Add a Test, and Add with AI Agent
- **Landing page cards** — RFC Requirement Dashboard card in hero section; Add a Test and Add with AI Agent cards in Contribute section
- **Glossary card** — RFC Requirement Dashboard linked from the glossary index page
- **Server configuration pages** — per-server docs pages showing Dockerfile, source code, and config files for all 36 tested servers (`docs/content/servers/`)
- **Clickable server names** — server names in the probe results table and summary bar chart now link to their configuration page
- **Sticky first column** — server name column stays pinned to the left edge while scrolling horizontally through result tables
- **Collapsible sub-groups** — group headers in result tables are now clickable to collapse/expand, with a chevron indicator and a "Collapse All / Expand All" toggle button
- **Row-click detail popup** — clicking a server row opens a modal showing that server's results for the current table in a vertical layout (Test, Expected, Got, Description) with section and table name in the header
- **Truncation notice** — tooltip and modal now show a `[Truncated]` notice at the top when raw request/response data exceeds the 8,192-byte display limit
- **Filter box** — text input above result tables to filter by server name, language, or test name; supports multiple comma-separated keywords
- **`--verbose` CLI flag** — prints the raw server response below each test result when enabled (`--verbose` or `-v`)
- **Giscus comments** — every glossary page now has a GitHub Discussions-powered comments section at the bottom

### Changed
- **Horizontal column headers** — test name headers are now displayed horizontally instead of rotated at -55°, improving readability
- **Zebra striping** — alternating row backgrounds for easier scanning
- **Softer borders** — lighter table borders in both light and dark mode, plus vertical separators between test columns
- **Expected row styling** — CSS-only background with dark mode support (fixes light gray leak in dark mode), thicker bottom border for visual separation
- **Scored/unscored separator** — heavier vertical border line between scored and unscored test columns
- **Larger pills** — increased padding, min-width, and border-radius for result badges
- **Improved readability** — larger base font (13px), bigger column headers with heavier weight and letter-spacing, more cell padding throughout
- **Group header refinement** — added padding and bottom border to collapsible group headers
- **Toggle button polish** — reduced border-radius from pill to button shape
- **Scroll overflow hint** — "Scroll to see all tests" label and right-edge fade gradient appear when tables overflow horizontally
- **Language suffix dark mode** — improved contrast for language labels in dark mode
- **Mobile bottom-sheet modal** — modal slides up from bottom on small screens with full width and rounded top corners
- **Touch-friendly targets** — larger buttons and invisible hit-area expansion on pills for touch devices
- **Smooth momentum scroll** — added `-webkit-overflow-scrolling:touch` for iOS
- **Stronger sticky shadow on mobile** — increased shadow intensity for the pinned server name column
- **Scrollable tooltips** — hover tooltips are now interactive and scrollable for large payloads (removed `pointer-events:none`, increased `max-height` to `60vh`)
- **Larger click modal** — expanded from `max-width:700px` to `90vw` and `max-height` from `80vh` to `85vh` to better accommodate large request/response data
- **Landing page section rename** — "Add Your Framework" heading renamed to "Contribute to the Project" with updated copy emphasizing community contributions
- **Uniform card sizing** — CSS rule forces all home page card grids to `repeat(2, 1fr)` so every card is the same width
- **Sidebar reordering** — RFC Requirement Dashboard at weight 2 (after Understanding HTTP), RFC Basics bumped to 3, Baseline to 4
- **Kestrel HEAD/OPTIONS support** — added explicit HEAD and OPTIONS endpoint handlers to ASP.NET Minimal server so smuggling tests evaluate correctly instead of returning 405
- **Add a Framework docs** — documented HEAD and OPTIONS as required endpoints
- Raw request capture now includes truncation metadata when payload exceeds 8,192 bytes (`TestRunner.cs`)
- Raw response capture now includes truncation metadata when response exceeds 8,192 bytes (`ResponseParser.cs`)

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
